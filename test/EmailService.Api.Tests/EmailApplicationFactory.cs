using EmailService.Internal;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using RegistryConstants = ServiceRegistryModules.ServiceRegistryModulesDefaults;
using EmailService.Api.IoC;
using Testcontainers.PostgreSql;
using Npgsql;

namespace EmailService.Api.Tests;

public sealed class EmailApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime {
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithDatabase("EmailServiceDb")
        .WithUsername("emailapitestuser")
        .WithPassword("emailapitestpassword")
        .Build();

    public FakeEmailClient EmailClient { get; private set; } = null!;
    public EmailDatabase Database { get; private set; } = null!;

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
            Database.Configuration = EmailClient.Configuration;
        });
    }

    #region IAsyncLifetime
    public async Task InitializeAsync() {
        await _dbContainer.StartAsync();
        Database = new EmailDatabase(_dbContainer.GetConnectionString());
    }

    async Task IAsyncLifetime.DisposeAsync() => await _dbContainer.StopAsync();
    #endregion

    #region Database Helpers
    public class EmailDatabase {
        private readonly string _connectionString;
        public EmailDatabase(string connectionString) => _connectionString = connectionString;

        public EmailConfiguration Configuration { get; set; } = null!;

        public async Task<EmailEntityDto?> GetEmailByIdAsync(string emailId, bool scrubResult = true) {
            if (!Guid.TryParse(emailId, out var id)) {
                return null;
            }

            EmailEntityDto? email = null;
            var sqlQuery = """
            SELECT data
            FROM   mt_doc_email
            WHERE  id = @Id
            """;
            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sqlQuery, connection);
            command.Parameters.AddWithValue("Id", id);
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync()) {
                var data = reader.GetString(0);
                email = EmailEntityDto.FromJson(data);
            }
            if (scrubResult) {
                ScrubEmail(email);
            }
            return email;
        }

        private void ScrubEmail(EmailEntityDto? email) {
            if (email is null) {
                return;
            }
            email.ScrubSender(Configuration);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync() {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
    #endregion
}
