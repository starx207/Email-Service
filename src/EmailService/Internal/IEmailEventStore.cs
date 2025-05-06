namespace EmailService.Internal;

internal interface IEmailEventStore {
    Guid SubmitEmail(string recipient, string subject, string body);
    void RecordEmailSent(Guid emailId);
    void RecordEmailFailed(Guid emailId);

    Task SaveEventsAsync(CancellationToken cancellationToken);
}