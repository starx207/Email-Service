using EmailService.Api.Contracts.Requests;
using EmailService.Models;
using System.Net.Mime;

namespace EmailService.Api.Endpoints.Email;

public static class SendEmailEndpoint {
    public const string NAME = "SendEmail";

    // TODO: We should put some validation in place for the requests. I skipped that for the sake of time.
    public static void MapSendEmailEndpoint(this IEndpointRouteBuilder app)
    => app.MapPost(ApiEndpoints.Email.SEND, async (IEmailService emailService, SendEmailRequest request, CancellationToken token) => {
        var response = await emailService.SendEmailAsync(request.To, request.Subject, request.Body, token);

        return response.Match(
            emailId => Results.Text(emailId.ToString()),
            errs => Results.BadRequest(errs)
        );
    })
        .WithName(NAME)
        .Produces<string>(StatusCodes.Status200OK, MediaTypeNames.Text.Plain)
        .Produces<ValidationErr[]>(StatusCodes.Status400BadRequest)
        .WithTags("Email");
}
