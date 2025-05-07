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

    // TODO: Probably should have another hosted service that will check the database for emails that
    //       need to be retried. It could just run every couple of hours or so.
    //       It would look for emails with a status of "pending" or "failed" that are older than a certain
    //       amount of time. It would then submit them to the queue again.
    //       For this to work, we would need to include the current retry attempt of the email when sending
    //       it to the queue. The email sender would then need to accept the retry attempt and modify the
    //       retry policy to only retry the max number of times minus the current attempt.
    //       If only one attemp is left, we could bypass the retry policy creation.
    //
    //       The reason for needing this is if the email service is down for a period of time and the in-flight
    //       emails are left in a "pending" or "failed" state (i.e. the retry policy was interrupted).

    // TODO: A good future enhancement would be to allow this to process emails in parallel. Right now,
    //       it only does one at a time (including the retry logic). This is a good start though.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (await _submittedChannel.Reader.WaitToReadAsync(stoppingToken)) {
            var queued = await _submittedChannel.Reader.ReadAsync(stoppingToken);

            // Start the stream for the email submission and write the email id back to the assigned channel.
            // Doing this in a dedicated scope so the session can be properly cleaned up before we start
            // a new session for the email sending/retry logic.
            // Not sure this is necessary, but I've been burned too many times by now (TODO: insert angry emoji here)
            Guid emailId;
            await using (var streamStartScope = _serviceProvider.CreateAsyncScope()) {
                var emailStore = streamStartScope.ServiceProvider.GetRequiredService<IEmailEventStore>();
                emailId = emailStore.SubmitEmail(queued.Recipient, queued.Subject, queued.Message);
                var assignment = new EmailIdAssigned {
                    TraceId = queued.TraceId,
                    EmailId = emailId
                };
                await _assignedChannel.Writer.WriteAsync(assignment, stoppingToken);

                // Save the initial email submission
                await emailStore.SaveEventsAsync(stoppingToken);
            }

            // Now attempt to send the email
            await using var sendEmailScope = _serviceProvider.CreateAsyncScope();
            var emailSender = sendEmailScope.ServiceProvider.GetRequiredService<IEmailSender>();
            await emailSender.SendAsync(emailId, queued.Recipient, queued.Subject, queued.Message, stoppingToken);
        }
    }
}
