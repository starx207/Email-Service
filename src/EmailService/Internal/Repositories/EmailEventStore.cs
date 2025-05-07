using EmailService.Events;
using EmailService.Models;
using Marten;

namespace EmailService.Internal.Repositories;

internal sealed class EmailEventStore : IEmailEventStore {
    private readonly IDocumentStore _docStore;
    private readonly EmailConfiguration _emailConfig;

    public EmailEventStore(IDocumentStore docStore, EmailConfiguration emailConfig) {
        _docStore = docStore;
        _emailConfig = emailConfig;
    }

    public async Task<Guid> SubmitEmailAsync(string recipient, string subject, string body, CancellationToken cancellationToken) {
        using var session = _docStore.LightweightSession();
        var submitted = new EmailSubmitted(_emailConfig.Sender, recipient, subject, body, DateTime.UtcNow);
        var streamId = session.Events.StartStream<Email>(submitted).Id;
        await session.SaveChangesAsync(cancellationToken);
        return streamId;
    }

    public async Task RecordEmailSentAsync(Guid emailId, CancellationToken cancellationToken) {
        using var session = _docStore.LightweightSession();
        var sent = new EmailSent(DateTime.UtcNow);
        session.Events.Append(emailId, sent);
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordEmailFailedAsync(Guid emailId, CancellationToken cancellationToken) {
        using var session = _docStore.LightweightSession();
        var failed = new EmailSendAttemptFailed(DateTime.UtcNow);
        session.Events.Append(emailId, failed);
        await session.SaveChangesAsync(cancellationToken);
    }

    public IEmailEventSession CreateAsyncSession() => new EmailEventSession(_docStore, _emailConfig);
}
