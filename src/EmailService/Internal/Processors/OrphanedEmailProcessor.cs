using System;
using System.Threading.Channels;
using EmailService.Internal.Dto;
using EmailService.Models;
using Marten;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Geometries;

namespace EmailService.Internal.Processors;

internal class OrphanedEmailProcessor : BackgroundService {
    private readonly Channel<EmailQueued> _emailQueue;
    private readonly IDocumentStore _docStore;

    public OrphanedEmailProcessor(Channel<EmailQueued> emailQueue, IDocumentStore docStore)
    {
        _emailQueue = emailQueue;
        _docStore = docStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            await using var session = _docStore.QuerySession();

            // Check for pending or failed emails that have been in the system for more than 1 hour
            // and resubmit them to the queue for processing.
            var offset = new DateTimeOffset(DateTime.UtcNow.AddHours(-1));
            var orphans = await session.Query<Email>()
                .Where(e => (e.Status == EmailStatus.Pending || e.Status == EmailStatus.Failed) 
                    && e.Retries < EmailConstants.MAX_RETRIES
                    && e.SubmittedOn < offset)
                .ToListAsync(stoppingToken);

            var resubmissions = orphans.Select(email => new EmailQueued {
                ExistingEmailId = email.Id,
                CurrentRetryCount = email.Retries,
                Recipient = email.Recipient,
                Subject = email.Subject ?? string.Empty,
                Message = email.Body ?? string.Empty
            }).ToList();

            foreach (var email in resubmissions) {
                // Send the email to the queue for processing
                await _emailQueue.Writer.WriteAsync(email, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
