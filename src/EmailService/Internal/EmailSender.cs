using System;
using MailKit;
using MailKit.Security;
using Microsoft.Extensions.Logging;

namespace EmailService.Internal;

internal sealed class EmailSender : IEmailSender {
    private readonly IEmailClient _emailClient;
    private readonly EmailConfiguration _emailConfig;
    private readonly ILogger<EmailSender>? _logger;

    public EmailSender(IEmailClient emailClient, EmailConfiguration emailConfig) {
        _emailClient = emailClient;
        _emailConfig = emailConfig;
    }

    public EmailSender(IEmailClient emailClient, EmailConfiguration emailConfig, ILogger<EmailSender> logger)
        : this(emailClient, emailConfig)
        => _logger = logger;

    public async Task<string> SendAsync(string recipient, string subject, string message, CancellationToken cancellationToken = default) {
        var mailId = Guid.NewGuid().ToString();

        var mailMessage = new MimeKit.MimeMessage {
            Subject = subject,
            Body = new MimeKit.TextPart(MimeKit.Text.TextFormat.Plain) { Text = message }
        };
        mailMessage.From.Add(new MimeKit.MailboxAddress(_emailConfig.Sender, _emailConfig.Sender));
        mailMessage.To.Add(new MimeKit.MailboxAddress(recipient, recipient));

        try {
            var socketOptions = Enum.TryParse<SecureSocketOptions>(_emailConfig.SecureSocket, ignoreCase: true, out var sso) ? sso : SecureSocketOptions.Auto;
            await _emailClient.ConnectAsync(_emailConfig.Host, _emailConfig.Port, socketOptions, cancellationToken);
            await _emailClient.AuthenticateAsync(_emailConfig.Sender, _emailConfig.Password, cancellationToken);
            await _emailClient.SendAsync(mailMessage, cancellationToken);
            await _emailClient.DisconnectAsync(true, cancellationToken);
        } catch (Exception ex) {
            // TODO: Set the status of the email to failed.
            _logger?.LogEmailSendFailed(recipient, subject, ex);
        }
        return mailId;
    }
}
