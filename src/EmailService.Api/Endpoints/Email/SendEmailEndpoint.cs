using EmailService.Api.Contracts.Requests;
using System.Net.Mime;

namespace EmailService.Api.Endpoints.Email;

public static class SendEmailEndpoint {
    public const string NAME = "SendEmail";

    public static void MapSendEmailEndpoint(this IEndpointRouteBuilder app)
    => app.MapPost(ApiEndpoints.Email.SEND, async (HttpContext http, IEmailSender emailSender, SendEmailRequest request, CancellationToken token) => {
        var emailId = await emailSender.SendAsync(request.To, request.Subject, request.Body, token);

        return Results.Text(emailId);
    })
        .WithName(NAME)
        .Produces<string>(StatusCodes.Status200OK, MediaTypeNames.Text.Plain)
        .Produces(StatusCodes.Status400BadRequest)
        .WithTags("Email");
}