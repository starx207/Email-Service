using System.Threading.Channels;
using EmailService.Internal.Dto;
using EmailService.Internal.Repositories;
using EmailService.Internal.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EmailService.Internal.Processors;

internal sealed class EmailQueueProcessor : BackgroundService {
    private readonly Channel<EmailQueued> _submittedChannel;
    private readonly Channel<EmailIdAssigned> _assignedChannel;
    private readonly IServiceProvider _serviceProvider;

    public EmailQueueProcessor(Channel<EmailQueued> submittedChannel, Channel<EmailIdAssigned> assignedChannel, IServiceProvider serviceProvider) {
        _submittedChannel = submittedChannel;
        _assignedChannel = assignedChannel;
        _serviceProvider = serviceProvider;
    }

    // TODO: Time-permitting, I should have another hosted service that will check the database for emails that
    //       need to be retried. It could just run every couple of hours or so.
    //       It would look for emails with a status of "pending" or "failed" that are older than a certain
    //       amount of time. It would then submit them to the queue again.
    //       For this to work, I would need to include the current retry attempt of the email when sending
    //       it to the queue. The email sender would then need to accept the retry attempt and modify the
    //       retry policy to only retry the max number of times minus the current attempt.
    //       If only one attemp is left, we could bypass the retry policy creation.
    //
    //       The reason for needing this is if the email service is down for a period of time and the in-flight
    //       emails are left in a "pending" or "failed" state (i.e. the retry policy was interrupted).

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(5); // Limit to 5 concurrent tasks. Could make this configurable later on.

        try {
            while (await _submittedChannel.Reader.WaitToReadAsync(stoppingToken)) {
                var queued = await _submittedChannel.Reader.ReadAsync(stoppingToken);

                Guid emailId;
                await using (var scope = _serviceProvider.CreateAsyncScope()) {
                    var emailStore = scope.ServiceProvider.GetRequiredService<IEmailEventStore>();
                    await using var session = emailStore.CreateAsyncSession();
                    emailId = session.SubmitEmail(queued.Recipient, queued.Subject, queued.Message);
                    var assignment = new EmailIdAssigned {
                        TraceId = queued.TraceId,
                        EmailId = emailId
                    };
                    await _assignedChannel.Writer.WriteAsync(assignment, stoppingToken);
                }

                // Now that we have the email ID, we can send the email.
                // We do this in a separate task so that we can continue processing the queue.
                await semaphore.WaitAsync(stoppingToken);
                tasks.Add(Task.Run(async () => {
                    try {
                        await SendEmailAsync(emailId, queued, stoppingToken);
                    } finally {
                        semaphore.Release();
                    }
                }, stoppingToken));
            }
        } catch (TaskCanceledException) {
            // This is expected when the application is shutting down. We can ignore this exception.
        } finally {
            // Wait for all tasks to complete before shutting down.
            await Task.WhenAll(tasks);
        }
    }

    private async Task SendEmailAsync(Guid emailId, EmailQueued queued, CancellationToken stoppingToken) {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        await emailSender.SendAsync(emailId, queued.Recipient, queued.Subject, queued.Message, stoppingToken);
    }    
}
