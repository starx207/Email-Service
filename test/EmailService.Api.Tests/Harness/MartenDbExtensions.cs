using EmailService.Events;
using EmailService.Models;
using Marten;
using Marten.Events.Projections;
using Weasel.Core;

namespace EmailService.Api.Tests.Harness;

public static class MartenDbExtensions
{
    public static async Task<Guid> CreateOrphanedEmailAsync(this EmailApplicationFactory.EmailDatabase database, Action<OrphanedEmailOptions>? options = null) {
        var orphanOpts = new OrphanedEmailOptions();
        options?.Invoke(orphanOpts);
        if (orphanOpts.SendAttempt > 3) {
            orphanOpts.SendAttempt = 3;
        }
        if (orphanOpts.LastAttempted.HasValue && orphanOpts.SendAttempt < 1) {
            orphanOpts.SendAttempt = 1;
        }
        if (orphanOpts.SendAttempt > 0 && !orphanOpts.LastAttempted.HasValue) {
            orphanOpts.LastAttempted = orphanOpts.Submitted.AddMinutes(orphanOpts.SendAttempt);
        }
        var orphanStatus = orphanOpts.LastAttempted.HasValue
            ? EmailStatus.Failed
            : EmailStatus.Pending;

        var store = DocumentStore.For(_ => {
            _.Connection(database.ConnectionString);
            _.AutoCreateSchemaObjects = AutoCreate.All;

            _.Events.AddEventType<EmailSubmitted>();
            _.Events.AddEventType<EmailSendAttemptFailed>();
            _.Events.AddEventType<EmailSent>();

            _.Projections.Add<EmailProjection>(ProjectionLifecycle.Inline);
        });

        await using var session = store.LightweightSession();
        var submitEmail = new EmailSubmitted(database.Configuration.Sender, orphanOpts.Recipient, orphanOpts.Subject, orphanOpts.Body, orphanOpts.Submitted);
        var emailId = session.Events.StartStream<Email>(submitEmail).Id;
        for (var i = 0; i < orphanOpts.SendAttempt; i++) {
            var sendAttempt = new EmailSendAttemptFailed(orphanOpts.LastAttempted ?? orphanOpts.Submitted);
            session.Events.Append(emailId, sendAttempt);
        }

        await session.SaveChangesAsync();
        return emailId;
    }
}

public class OrphanedEmailOptions {
    public string Recipient { get; set; } = "test@recipient.com";
    public string Subject { get; set; } = "Test Subject";
    public string Body { get; set; } = "Test Body";
    public DateTime Submitted { get; set; } = DateTime.UtcNow.AddDays(-1);
    public DateTime? LastAttempted { get; set; }
    public int SendAttempt { get; set; }
}
