using EmailService.Internal;

namespace EmailService;

public sealed class EmailConfiguration {
    public string Sender { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string SecureSocket { get; set; } = string.Empty;
    public string StoreConnectionString { get; set; } = string.Empty;
    public string? RetryDelay { get; set; }
    internal IEmailClient? CustomClientOverride { get; set; }

    public TimeSpan RetryDelayTimeSpan => string.IsNullOrEmpty(RetryDelay)
        ? TimeSpan.FromSeconds(15)
        : RetryDelay.ToTimeSpan();
}
