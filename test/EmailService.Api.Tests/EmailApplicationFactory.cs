using EmailService.Internal;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using RegistryConstants = ServiceRegistryModules.ServiceRegistryModulesDefaults;
using EmailService.Api.IoC;

namespace EmailService.Api.Tests;

public sealed class EmailApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime {
    public FakeEmailClient EmailClient { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        // Additional configuration before services are registered.
        // This will register our event handler to override the default email client with a fake one.
        var registryKey = string.Join(':',
            RegistryConstants.REGISTRIES_KEY,
            RegistryConstants.CONFIGURATION_KEY,
            nameof(EmailConfigurationRegistry),
            nameof(EmailConfigurationRegistry.Configuring) // This is the key that will be used to override the default email client 
        );
        var handlerName = $"{typeof(ServiceRegistryOverrides).FullName!}.{nameof(ServiceRegistryOverrides.OnConfiguringEmailClient)}";
        builder.UseSetting(registryKey, handlerName);

        // Now allow the application configuration to be created.
        base.ConfigureWebHost(builder);

        // Then extract the email client from the DI container.
        builder.ConfigureTestServices(services => {
            using var serviceProvider = services.BuildServiceProvider();
            EmailClient = (FakeEmailClient)serviceProvider.GetRequiredService<IEmailClient>();
            EmailClient.Configuration = serviceProvider.GetRequiredService<EmailConfiguration>();
        });
    }

    #region IAsyncLifetime
    // TODO: I will use these once I add testcontainers
    public Task InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
    #endregion
}
