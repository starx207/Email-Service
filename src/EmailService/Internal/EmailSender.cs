using MailKit.Security;
using Microsoft.Extensions.Logging;
using Polly;

namespace EmailService.Internal;

internal sealed class EmailSender : IEmailSender {
    private readonly IEmailClient _emailClient;
    private readonly EmailConfiguration _emailConfig;
    private readonly IEmailEventStore _emailStore;
    private readonly ILogger<EmailSender>? _logger;

    public EmailSender(IEmailClient emailClient, EmailConfiguration emailConfig, IEmailEventStore emailStore) {
        _emailClient = emailClient;
        _emailConfig = emailConfig;
        _emailStore = emailStore;
    }

    public EmailSender(IEmailClient emailClient, EmailConfiguration emailConfig, IEmailEventStore emailStore, ILogger<EmailSender> logger)
        : this(emailClient, emailConfig, emailStore)
        => _logger = logger;

    public async Task<string> SendAsync(string recipient, string subject, string message, CancellationToken cancellationToken = default) {
        var mailId = await _emailStore.SubmitEmailAsync(recipient, subject, message, cancellationToken);

        var mailMessage = new MimeKit.MimeMessage {
            Subject = subject,
            Body = new MimeKit.TextPart(MimeKit.Text.TextFormat.Plain) { Text = message }
        };
        mailMessage.From.Add(new MimeKit.MailboxAddress(_emailConfig.Sender, _emailConfig.Sender));
        mailMessage.To.Add(new MimeKit.MailboxAddress(recipient, recipient));

        // Configure the retry policy for sending the email.
        var delay = _emailConfig.RetryDelayTimeSpan;
        var retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: EmailConstants.MAX_RETIRES,
                sleepDurationProvider: attempt => delay * Math.Pow(2, attempt - 1),
                onRetry: async (ex, _) => {
                    await _emailStore.RecordEmailFailedAsync(mailId, cancellationToken);
                }
            );

        // Configure the fallback policy for when all retries fail.
        var fallbackPolicy = Policy.Handle<Exception>()
            .FallbackAsync(
                fallbackAction: _ => Task.CompletedTask,
                onFallbackAsync: ex => {
                    _logger?.LogEmailSendFailed(recipient, subject, ex);
                    return Task.CompletedTask;
                }
            );

        // Send the email with the retry and fallback policies applied.
        await fallbackPolicy.WrapAsync(retryPolicy).ExecuteAsync(async (token) => {
            var socketOptions = SocketOptionsFrom(_emailConfig.SecureSocket);
            await _emailClient.ConnectAsync(_emailConfig.Host, _emailConfig.Port, socketOptions, token);
            await _emailClient.AuthenticateAsync(_emailConfig.Sender, _emailConfig.Password, token);
            await _emailClient.SendAsync(mailMessage, token);
            await _emailClient.DisconnectAsync(true, token);

            await _emailStore.RecordEmailSentAsync(mailId, token);
        }, cancellationToken);

        return mailId.ToString();
    }

    public static SecureSocketOptions SocketOptionsFrom(string configuredValue)
        => Enum.TryParse<SecureSocketOptions>(configuredValue, ignoreCase: true, out var sso) ? sso : SecureSocketOptions.Auto;
}
