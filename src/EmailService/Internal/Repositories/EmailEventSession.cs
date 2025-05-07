using EmailService.Events;
using EmailService.Models;
using Marten;

namespace EmailService.Internal.Repositories;

internal sealed class EmailEventSession : IEmailEventSession {
    private readonly IDocumentSession _session;
    private readonly EmailConfiguration _emailConfig;

    public EmailEventSession(IDocumentStore docStore, EmailConfiguration emailConfig) {
        _session = docStore.LightweightSession();
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

    public async ValueTask DisposeAsync() {
        await _session.SaveChangesAsync();
        await _session.DisposeAsync();
    }
}
