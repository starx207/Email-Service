namespace EmailService.Internal.Services;

internal interface IEmailSender {
    Task SendAsync(Guid emailId, string recipient, string subject, string message, CancellationToken cancellationToken = default);
}
