namespace EmailService.Internal.Repositories;

internal interface IEmailEventStore {
    Task<Guid> SubmitEmailAsync(string recipient, string subject, string body, CancellationToken cancellationToken);
    Task RecordEmailSentAsync(Guid emailId, CancellationToken cancellationToken);
    Task RecordEmailFailedAsync(Guid emailId, CancellationToken cancellationToken);

    IEmailEventSession CreateAsyncSession();
}
