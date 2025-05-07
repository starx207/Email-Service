using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace EmailService.Api.Sdk;

public static class ServiceCollectionExtensions {
    public static IHttpClientBuilder AddEmailServiceApiClient(this IServiceCollection services)
        => services.AddRefitClient<IEmailServiceApi>()
        // Since we're running this in Docker, we'll use the service name and let docker resolve it.
        // To make this SDK production-ready, we'd have to use whatever URI we actually publish the API to.
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://emailservice.api"));
}
