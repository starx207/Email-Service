namespace EmailService.Internal.Dto;

internal sealed class EmailQueued {
    public Guid TraceId { get; set; } = Guid.NewGuid();
    public required string Recipient { get; set; }
    public required string Subject { get; set; }
    public required string Message { get; set; }
}
