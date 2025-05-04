using EmailService.Api.Contracts.Requests;

namespace EmailService.Api.Endpoints.Email;

public static class SendEmailEndpoint {
    public const string NAME = "SendEmail";

    public static void MapSendEmailEndpoint(this IEndpointRouteBuilder app)
    => app.MapPost(ApiEndpoints.Email.SEND, async (IEmailSender emailSender, SendEmailRequest request, CancellationToken token) => {
        var emailId = await emailSender.SendAsync(request.To, request.Subject, request.Body, token);
        return Results.Ok(emailId);
    })
        .WithName(NAME)
        .Produces<string>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithTags("Email");
}