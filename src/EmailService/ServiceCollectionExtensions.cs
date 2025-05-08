using System.Threading.Channels;
using EmailService.Events;
using EmailService.Internal.Dto;
using EmailService.Internal.Processors;
using EmailService.Internal.Repositories;
using EmailService.Internal.Services;
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
        services.AddEmailClient(emailConfig);
        services.AddEmailPersistence(emailConfig);

        services.AddTransient<IEmailService, Internal.Services.EmailService>();
        services.AddTransient<IEmailSender, EmailSender>();

        services.AddEmailProcessors();

        return services;
    }

    private static void AddEmailProcessors(this IServiceCollection services) {
        services.AddSingleton(_ => Channel.CreateUnbounded<EmailQueued>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        }));
        services.AddSingleton(_ => Channel.CreateUnbounded<EmailIdAssigned>(new UnboundedChannelOptions {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        }));
        services.AddHostedService<EmailQueueProcessor>();
        services.AddHostedService<OrphanedEmailProcessor>();
    }

    private static void AddEmailPersistence(this IServiceCollection services, EmailConfiguration emailConfig) {
        services.AddScoped<IEmailEventStore, EmailEventStore>();

        services.AddMarten(options => {

            options.Connection(emailConfig.StoreConnectionString);

            options.Events.AppendMode = EventAppendMode.Quick;

            options.Events.AddEventType<EmailSubmitted>();
            options.Events.AddEventType<EmailSendAttemptFailed>();
            options.Events.AddEventType<EmailSent>();

            options.Projections.Add<EmailProjection>(ProjectionLifecycle.Inline);

        }).UseLightweightSessions()
        .OptimizeArtifactWorkflow()
        // NOTE: Realistically, I wouldn't do this unless running in development. Production db changes ought to be handled differently
        .ApplyAllDatabaseChangesOnStartup();
    }

    private static void AddEmailClient(this IServiceCollection services, EmailConfiguration emailConfig) {
        services.AddSingleton(emailConfig);
        if (emailConfig.CustomClientOverride is not null) {
            services.AddSingleton(emailConfig.CustomClientOverride);
        } else {
            // Register the IEmailClient with a factory method to supply the ISmtpClient.
            // This way we avoid exposing the inner workings of ISmtpClient to the consumer of this library through IServiceCollection.
            services.AddTransient<IEmailClient>(_ => new DefaultEmailClient(new SmtpClient()));
        }
    }
}
