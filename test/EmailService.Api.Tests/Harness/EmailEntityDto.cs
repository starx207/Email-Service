using System.Text.Json;

namespace EmailService.Api.Tests.Harness;

public class EmailEntityDto {
    public Guid Id { get; set; }
    public string? Body { get; set; }
    public string? Sender { get; set; }
    public EmailStatus Status { get; set; }
    public string? StatusName { get; set; }
    public int Retries { get; set; }
    public string? Subject { get; set; }
    public string? Recipient { get; set; }
    public DateTime SubmittedOn { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? LastSendAttempt { get; set; }

    public EmailEntityDto ScrubSender(EmailConfiguration configuration) {
        Sender = configuration.Sender.Equals(Sender, StringComparison.OrdinalIgnoreCase)
            ? "{Configuration.Sender}"
            : "{Unexpected Sender}";
        return this;
    }

    #region Static
    internal static JsonSerializerOptions EmailEntitySerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    internal static EmailEntityDto? FromJson(string data) {
        try {
            var dto = JsonSerializer.Deserialize<EmailEntityDto>(data, EmailEntitySerializerOptions);
            if (dto is not null) {
                dto.StatusName = dto.Status.ToString();
            }
            return dto;
        } catch {
            return null;
        }
    }
    #endregion
}
