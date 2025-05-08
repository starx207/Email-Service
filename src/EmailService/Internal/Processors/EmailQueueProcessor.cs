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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(5); // Limit to 5 concurrent tasks. Could make this configurable later on.

        try {
            while (await _submittedChannel.Reader.WaitToReadAsync(stoppingToken)) {
                var queued = await _submittedChannel.Reader.ReadAsync(stoppingToken);

                Guid emailId;
                if (queued.ExistingEmailId.HasValue) {
                    emailId = queued.ExistingEmailId.Value;
                } else {
                    // The email ID is not known yet. We need to create a new email record in the database.
                    await using var scope = _serviceProvider.CreateAsyncScope();
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
        await emailSender.SendAsync(emailId, queued.Recipient, queued.Subject, queued.Message, queued.CurrentRetryCount, stoppingToken);
    }    
}
