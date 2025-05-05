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

        var attempt = 0;
        var retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: EmailConstants.MAX_RETIRES,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt - 1)),
                onRetry: async (ex, _) => {
                    attempt++; // Not sure this is the correct way to do this, but I can't find anything in the Context that would tell me the attempt number.
                    await _emailStore.RecordEmailFailedAsync(mailId, cancellationToken);
                    if (attempt >= EmailConstants.MAX_RETIRES) {
                        _logger?.LogEmailSendFailed(recipient, subject, ex);
                    }
                }
            );

        try {
            await retryPolicy.ExecuteAsync(async (token) => {
                var socketOptions = SocketOptionsFrom(_emailConfig.SecureSocket);
                await _emailClient.ConnectAsync(_emailConfig.Host, _emailConfig.Port, socketOptions, cancellationToken);
                await _emailClient.AuthenticateAsync(_emailConfig.Sender, _emailConfig.Password, cancellationToken);
                await _emailClient.SendAsync(mailMessage, cancellationToken);
                await _emailClient.DisconnectAsync(true, cancellationToken);

                await _emailStore.RecordEmailSentAsync(mailId, cancellationToken);
            }, cancellationToken);
        } catch {
            // If we still fail after all retries, just move on.
            // We've already logged the error and updated the email status in the retry callback.
        }

        return mailId.ToString();
    }

    public static SecureSocketOptions SocketOptionsFrom(string configuredValue)
        => Enum.TryParse<SecureSocketOptions>(configuredValue, ignoreCase: true, out var sso) ? sso : SecureSocketOptions.Auto;
}
