using EmailService.Internal;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using RegistryConstants = ServiceRegistryModules.ServiceRegistryModulesDefaults;
using EmailService.Api.IoC;
using Testcontainers.PostgreSql;

namespace EmailService.Api.Tests;

public sealed class EmailApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime {
    public FakeEmailClient EmailClient { get; private set; } = null!;
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithDatabase("EmailServiceDb")
        .WithUsername("emailapitestuser")
        .WithPassword("emailapitestpassword")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        // Additional configuration before services are registered.
        // This will register our event handler to override the default email client with a fake one.
        var registryKey = string.Join(':',
            RegistryConstants.REGISTRIES_KEY,
            RegistryConstants.CONFIGURATION_KEY,
            nameof(EmailConfigurationRegistry)
        );
        var handlerName = $"{typeof(ServiceRegistryOverrides).FullName!}.{nameof(ServiceRegistryOverrides.OnConfiguringEmailClient)}";
        builder.UseSetting($"{registryKey}:{nameof(EmailConfigurationRegistry.Configuring)}", handlerName);
        builder.UseSetting($"{registryKey}:{nameof(EmailConfigurationRegistry.ConnectionString)}", _dbContainer.GetConnectionString());

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
    public async Task InitializeAsync() => await _dbContainer.StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await _dbContainer.StopAsync();
    #endregion
}
