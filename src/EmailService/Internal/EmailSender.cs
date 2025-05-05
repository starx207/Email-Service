using MailKit.Security;
using Microsoft.Extensions.Logging;

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

    // TODO: I'm considering using Polly for the retry logic here.
    //       I'm also considering pulling the save change logic out of each IEmailEventStore method and just doing it once
    //       at the end of the operation. This would negate the need for an async OnRetry callback for Polly (since all the docs show non-async callbacks).
    public async Task<string> SendAsync(string recipient, string subject, string message, CancellationToken cancellationToken = default) {
        var mailId = await _emailStore.SubmitEmailAsync(recipient, subject, message, cancellationToken);

        var mailMessage = new MimeKit.MimeMessage {
            Subject = subject,
            Body = new MimeKit.TextPart(MimeKit.Text.TextFormat.Plain) { Text = message }
        };
        mailMessage.From.Add(new MimeKit.MailboxAddress(_emailConfig.Sender, _emailConfig.Sender));
        mailMessage.To.Add(new MimeKit.MailboxAddress(recipient, recipient));

        try {
            var socketOptions = SocketOptionsFrom(_emailConfig.SecureSocket);
            await _emailClient.ConnectAsync(_emailConfig.Host, _emailConfig.Port, socketOptions, cancellationToken);
            await _emailClient.AuthenticateAsync(_emailConfig.Sender, _emailConfig.Password, cancellationToken);
            await _emailClient.SendAsync(mailMessage, cancellationToken);
            await _emailClient.DisconnectAsync(true, cancellationToken);

            await _emailStore.RecordEmailSentAsync(mailId, cancellationToken);
        } catch (Exception ex) {
            _logger?.LogEmailSendFailed(recipient, subject, ex);
            await _emailStore.RecordEmailFailedAsync(mailId, cancellationToken);
        }
        return mailId.ToString();
    }

    public static SecureSocketOptions SocketOptionsFrom(string configuredValue)
        => Enum.TryParse<SecureSocketOptions>(configuredValue, ignoreCase: true, out var sso) ? sso : SecureSocketOptions.Auto;
}
