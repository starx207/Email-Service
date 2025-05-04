namespace EmailService.Api.Contracts.Requests;

public class SendEmailRequest {
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
}
