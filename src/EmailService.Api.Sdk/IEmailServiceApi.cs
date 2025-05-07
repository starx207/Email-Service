using EmailService.Api.Contracts.Requests;
using Refit;

namespace EmailService.Api.Sdk;

public interface IEmailServiceApi {
    [Post(ApiEndpoints.Email.SEND)]
    Task<string> SendEmailAsync(SendEmailRequest request);
}
