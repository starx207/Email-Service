using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EmailService.Internal;

// This class is just a wrapper for MailKit ISmtpClient.
// The primary reason for this is so we provide alernate implementations of IEmailClient
// in tests without having to implement all the details of ISmtpClient.
internal class DefaultEmailClient : IEmailClient {
    private readonly ISmtpClient _smtp;

    public DefaultEmailClient(ISmtpClient smtp) => _smtp = smtp;

    public Task AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default)
        => _smtp.AuthenticateAsync(userName, password, cancellationToken);
    public Task ConnectAsync(string host, int port, SecureSocketOptions socketOptions, CancellationToken cancellationToken = default)
        => _smtp.ConnectAsync(host, port, socketOptions, cancellationToken);
    public Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default)
        => _smtp.DisconnectAsync(quit, cancellationToken);
    public Task SendAsync(MimeMessage message, CancellationToken cancellationToken = default)
        => _smtp.SendAsync(message, cancellationToken);
}
