using EmailService.Internal;
using MailKit.Security;
using MimeKit;

namespace EmailService.Api.Tests;

public class FakeEmailClient : IEmailClient {
    private bool _authenticated;
    private bool _connected;

    public EmailConfiguration Configuration { get; set; } = null!;
    public List<SentEmail> SentEmails { get; } = [];

    public virtual Task AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default) {
        _authenticated = string.Equals(userName, Configuration.Sender) && string.Equals(password, Configuration.Password);
        return Task.CompletedTask;
    }

    public virtual Task ConnectAsync(string host, int port, SecureSocketOptions socketOptions, CancellationToken cancellationToken = default) {
        var parsedOptions = EmailSender.SocketOptionsFrom(Configuration.SecureSocket);
        _connected = string.Equals(host, Configuration.Host) && port == Configuration.Port && socketOptions == parsedOptions;
        return Task.CompletedTask;
    }

    public virtual Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default) {
        if (quit) {
            _authenticated = false;
            _connected = false;
        }
        return Task.CompletedTask;
    }

    public virtual Task SendAsync(MimeMessage message, CancellationToken cancellationToken = default) {
        if (!(_connected && _authenticated)) {
            throw new InvalidOperationException("Client is not connected or authenticated.");
        }

        var recipient = message.To.Mailboxes.FirstOrDefault()?.Address ?? string.Empty;
        var subject = message.Subject;
        var body = message.TextBody;

        SentEmails.Add(new(recipient, subject, body));

        return Task.CompletedTask;
    }
}

public record SentEmail(string To, string Subject, string Body);