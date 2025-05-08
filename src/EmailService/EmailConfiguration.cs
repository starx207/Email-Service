using EmailService.Internal.Extensions;
using EmailService.Internal.Services;

namespace EmailService;

public sealed class EmailConfiguration {
    // TODO: We should really have validation on anything the consumer is giving as input. I skipped that for the sake of time.
    public string Sender { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string SecureSocket { get; set; } = string.Empty;
    public string StoreConnectionString { get; set; } = string.Empty;
    public string? RetryDelay { get; set; }
    internal IEmailClient? CustomClientOverride { get; set; }

    internal TimeSpan RetryDelayTimeSpan => string.IsNullOrEmpty(RetryDelay)
        ? TimeSpan.FromSeconds(15)
        : RetryDelay.ToTimeSpan();
}
