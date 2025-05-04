using EmailService.Internal;
using MailKit.Net.Smtp;
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
        } else {
            // Register the IEmailClient with a factory method to supply the ISmtpClient.
            // This way we avoid exposing the inner workings of ISmtpClient to the consumer of this library through IServiceCollection.
            services.AddTransient<IEmailClient>(_ => new DefaultEmailClient(new SmtpClient()));
        }

        services.AddTransient<IEmailSender, EmailSender>();


        return services;
    }
}
