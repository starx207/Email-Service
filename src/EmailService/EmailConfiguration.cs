using EmailService.Internal;

namespace EmailService;

public sealed class EmailConfiguration {
    public string? From { get; set; }
    public string? Password { get; set; }
    public string? Host { get; internal set; }
    public int Port { get; internal set; }
    public string? SecureSocket { get; internal set; }
    internal IEmailClient? CustomClientOverride { get; set; }
}
