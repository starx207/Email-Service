using EmailService.Events;
using EmailService.Internal;
using MailKit.Net.Smtp;
using Marten;
using Marten.Events;
using Marten.Events.Projections;
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
        services.AddTransient<IEmailEventStore, EmailEventStore>();

        services.AddMarten(options => {

            options.Connection(emailConfig.StoreConnectionString);

            options.Events.AppendMode = EventAppendMode.Quick;

            options.Events.AddEventType<EmailSubmitted>();
            options.Events.AddEventType<EmailSendAttemptFailed>();
            options.Events.AddEventType<EmailSent>();

            options.Projections.Add<EmailProjection>(ProjectionLifecycle.Inline);

        }).UseLightweightSessions()
        .OptimizeArtifactWorkflow();

        return services;
    }
}
