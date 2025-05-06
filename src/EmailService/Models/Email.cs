namespace EmailService.Models;

public sealed class Email {
    public Guid Id { get; set; }
    public required string Sender { get; set; }
    public required string Recipient { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public DateTime SubmittedOn { get; set; } = DateTime.UtcNow;
    public DateTime? LastSendAttempt { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public int Retries { get; set; }
}
