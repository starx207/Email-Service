using Microsoft.Extensions.Logging;

namespace EmailService.Internal.Extensions;

internal static partial class LoggerExtensions {
    
    [LoggerMessage(
        Message = "Failed to send email to {Recipient} with subject {Subject}",
        EventName = "EmailSendFailed",
        Level = LogLevel.Error)]
    public static partial void LogEmailSendFailed(this ILogger logger, string recipient, string subject, Exception exception);
}
