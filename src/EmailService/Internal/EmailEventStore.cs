using EmailService.Events;
using EmailService.Models;
using Marten;

namespace EmailService.Internal;

internal sealed class EmailEventStore : IEmailEventStore {
    private readonly IDocumentSession _session;
    private readonly EmailConfiguration _emailConfig;

    public EmailEventStore(IDocumentSession session, EmailConfiguration emailConfig) {
        _session = session;
        _emailConfig = emailConfig;
    }

    public async Task<Guid> SubmitEmailAsync(string recipient, string subject, string body, CancellationToken cancellationToken) {
        var submitted = new EmailSubmitted(Guid.NewGuid(), _emailConfig.Sender, recipient, subject, body, DateTime.UtcNow);
        var streamId = _session.Events.StartStream<Email>(submitted.EmailId, submitted).Id;
        await _session.SaveChangesAsync(cancellationToken);
        return streamId;
    }

    public async Task RecordEmailSentAsync(Guid emailId, CancellationToken cancellationToken) {
        var sent = new EmailSent(emailId, DateTime.UtcNow);
        _session.Events.Append(emailId, sent);
        await _session.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordEmailFailedAsync(Guid emailId, CancellationToken cancellationToken) {
        var failed = new EmailSendAttemptFailed(emailId, DateTime.UtcNow);
        _session.Events.Append(emailId, failed);
        await _session.SaveChangesAsync(cancellationToken);
    }
}
