using Microsoft.Extensions.DependencyInjection;

namespace EmailService;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddEmailService(this IServiceCollection services, Action<EmailConfiguration>? configure = null) {
        var emailConfig = new EmailConfiguration();
        configure?.Invoke(emailConfig);
        return services.AddEmailService(emailConfig);
    }

    public static IServiceCollection AddEmailService(this IServiceCollection services, EmailConfiguration emailConfig) {
        services.AddSingleton(emailConfig);
        if (emailConfig.CustomClientOverride is not null) {
            services.AddSingleton(emailConfig.CustomClientOverride);
        }
        else {
            // TODO: Still need to define the default client
        }
        return services;
    }
}
