using EmailService.Api.Contracts.Requests;
using System.Net.Mime;

namespace EmailService.Api.Endpoints.Email;

public static class SendEmailEndpoint {
    public const string NAME = "SendEmail";

    // TODO: We should put some validation in place for the requests. I skipped that for the sake of time.
    public static void MapSendEmailEndpoint(this IEndpointRouteBuilder app)
    => app.MapPost(ApiEndpoints.Email.SEND, async (IEmailService emailService, SendEmailRequest request, CancellationToken token) => {
        var emailId = await emailService.SendEmailAsync(request.To, request.Subject, request.Body, token);

        return Results.Text(emailId.ToString());
    })
        .WithName(NAME)
        .Produces<string>(StatusCodes.Status200OK, MediaTypeNames.Text.Plain)
        .Produces(StatusCodes.Status400BadRequest)
        .WithTags("Email");
}
