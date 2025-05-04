namespace EmailService.Api.Endpoints.Email;

public static class EmailEndpointExtensions {
    public static void MapEmailEndpoints(this IEndpointRouteBuilder app)
        => app.MapSendEmailEndpoint();
}
