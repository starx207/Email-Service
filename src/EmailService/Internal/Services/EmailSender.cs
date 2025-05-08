using EmailService.Internal.Extensions;
using EmailService.Internal.Repositories;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Polly;

namespace EmailService.Internal.Services;

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

    public async Task SendAsync(Guid emailId, string recipient, string subject, string message, int currentRetryAttempt = 0, CancellationToken cancellationToken = default) {
        var mailMessage = new MimeKit.MimeMessage {
            Subject = subject,
            Body = new MimeKit.TextPart(MimeKit.Text.TextFormat.Plain) { Text = message }
        };
        mailMessage.From.Add(new MimeKit.MailboxAddress(_emailConfig.Sender, _emailConfig.Sender));
        mailMessage.To.Add(new MimeKit.MailboxAddress(recipient, recipient));

        // Configure the retry policy for sending the email.
        var retryPolicy = CreateRetryPolicy(emailId, currentRetryAttempt, cancellationToken);
        // Configure the fallback policy for when all retries fail.
        var fallbackPolicy = CreateFallbackPolicy(emailId, recipient, subject, cancellationToken);
        // If the retry policy is null, we only need the fallback policy.
        var finalPolicy = retryPolicy is null ? fallbackPolicy : fallbackPolicy.WrapAsync(retryPolicy);

        // Send the email with the retry and fallback policies applied.
        await finalPolicy.ExecuteAsync(async (token) => {
            var socketOptions = SocketOptionsFrom(_emailConfig.SecureSocket);
            await _emailClient.ConnectAsync(_emailConfig.Host, _emailConfig.Port, socketOptions, token);
            await _emailClient.AuthenticateAsync(_emailConfig.Sender, _emailConfig.Password, token);
            await _emailClient.SendAsync(mailMessage, token);
            await _emailClient.DisconnectAsync(true, token);

            await _emailStore.RecordEmailSentAsync(emailId, token);
        }, cancellationToken);
    }

    public static SecureSocketOptions SocketOptionsFrom(string configuredValue)
        => Enum.TryParse<SecureSocketOptions>(configuredValue, ignoreCase: true, out var sso) ? sso : SecureSocketOptions.Auto;

    private IAsyncPolicy CreateFallbackPolicy(Guid emailId, string recipient, string subject, CancellationToken token)
        => Policy.Handle<Exception>()
        .FallbackAsync(
            fallbackAction: _ => Task.CompletedTask,
            onFallbackAsync: async ex => {
                await _emailStore.RecordEmailFailedAsync(emailId, token);
                _logger?.LogEmailSendFailed(recipient, subject, ex);
            }
        );

    private IAsyncPolicy? CreateRetryPolicy(Guid emailId, int currentRetryAttempt, CancellationToken token) {
        if (currentRetryAttempt == EmailConstants.MAX_RETRIES - 1) {
            return null; // Only 1 retry left, no need for a retry policy.
        }

        var delay = _emailConfig.RetryDelayTimeSpan;
        return Policy.Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: EmailConstants.MAX_RETRIES - currentRetryAttempt,
                sleepDurationProvider: attempt => delay * Math.Pow(2, attempt - 1),
                onRetryAsync: async (_, _) => {
                    await _emailStore.RecordEmailFailedAsync(emailId, token);
                }
            );
    }
}
