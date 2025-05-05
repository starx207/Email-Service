using System.Text.Json;

namespace EmailService.Api.Tests;

public class EmailEntityDto {
    public Guid Id { get; set; }
    public string? Body { get; set; }
    public string? Sender { get; set; }
    public EmailStatus Status { get; set; }
    public int Retries { get; set; }
    public string? Subject { get; set; }
    public string? Recipient { get; set; }
    public DateTime SubmittedOn { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? LastSendAttempt { get; set; }

    public void ScrubSender(EmailConfiguration configuration)
        => Sender = configuration.Sender.Equals(Sender, StringComparison.OrdinalIgnoreCase)
        ? "{Configuration.Sender}"
        : "{Unexpected Sender}";

    #region Static
    internal static JsonSerializerOptions EmailEntitySerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    internal static EmailEntityDto? FromJson(string data) {
        try {
            return JsonSerializer.Deserialize<EmailEntityDto>(data, EmailEntitySerializerOptions);
        } catch {
            return null;
        }
    }
    #endregion
}
