namespace EmailService.Api.Middleware;

public static class ExceptionMiddlewareExtensions {
    public static void UseAppExceptionHandling(this IApplicationBuilder app)
        => app.Use(static async (context, next) => {
            try {
                await next(context);
            } catch (Exception ex) {
                var logger = context.RequestServices.GetService<ILogger<Program>>();
                logger?.LogError(ex, "An unhandled exception occurred while processing the request.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
            }
        });
}
