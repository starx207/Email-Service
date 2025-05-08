namespace EmailService.Internal.Dto;

internal sealed class EmailQueued {
    public Guid TraceId { get; set; } = Guid.NewGuid();
    public Guid? ExistingEmailId { get; set; }
    public int CurrentRetryCount { get; set; }
    public required string Recipient { get; set; }
    public required string Subject { get; set; }
    public required string Message { get; set; }
}
