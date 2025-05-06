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

    public Guid SubmitEmail(string recipient, string subject, string body) {
        var submitted = new EmailSubmitted(_emailConfig.Sender, recipient, subject, body, DateTime.UtcNow);
        var streamId = _session.Events.StartStream<Email>(submitted).Id;
        return streamId;
    }

    public void RecordEmailSent(Guid emailId) {
        var sent = new EmailSent(DateTime.UtcNow);
        _session.Events.Append(emailId, sent);
    }

    public void RecordEmailFailed(Guid emailId) {
        var failed = new EmailSendAttemptFailed(DateTime.UtcNow);
        _session.Events.Append(emailId, failed);
    }

    public async Task SaveEventsAsync(CancellationToken cancellationToken)
        => await _session.SaveChangesAsync(cancellationToken);
}
