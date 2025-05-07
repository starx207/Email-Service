namespace EmailService.Internal.Repositories;

internal interface IEmailEventSession : IAsyncDisposable {
    void RecordEmailFailed(Guid emailId);
    void RecordEmailSent(Guid emailId);
    Guid SubmitEmail(string recipient, string subject, string body);
}
