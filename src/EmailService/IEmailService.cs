using EmailService.Models;

namespace EmailService;
public interface IEmailService {
    /// <summary>
    /// Queues a message to be sent asychronously.
    /// </summary>
    /// <returns>A mail Id that can be used to check the status of the email</returns>
    Task<Result<Guid, ValidationErr[]>> SendEmailAsync(string recipient, string subject, string message, CancellationToken cancellationToken = default);
}