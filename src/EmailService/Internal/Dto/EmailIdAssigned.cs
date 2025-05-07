namespace EmailService.Internal.Dto;

internal sealed class EmailIdAssigned {
    public required Guid TraceId { get; set; }
    public required Guid EmailId { get; set; }
}
