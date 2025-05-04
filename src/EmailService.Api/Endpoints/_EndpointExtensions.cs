using EmailService.Api.Endpoints.Email;

namespace EmailService.Api.Endpoints;

public static class EndpointExtensions {
    public static void MapEndpoints(this IEndpointRouteBuilder app)
        => app.MapEmailEndpoints();
}
