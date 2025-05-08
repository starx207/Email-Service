using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using RegistryConstants = ServiceRegistryModules.ServiceRegistryModulesDefaults;
using EmailService.Api.IoC;
using Testcontainers.PostgreSql;
using Npgsql;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using EmailService.Internal.Services;

namespace EmailService.Api.Tests.Harness;

public sealed class EmailApplicationDatabaseProvider : IAsyncLifetime {
    public PostgreSqlContainer DbContainer { get; } = new PostgreSqlBuilder()
        .WithDatabase("EmailServiceDb")
        .WithUsername("emailapitestuser")
        .WithPassword("emailapitestpassword")
        .Build();

    public async Task InitializeAsync() => await DbContainer.StartAsync();
    async Task IAsyncLifetime.DisposeAsync() => await DbContainer.StopAsync();
}

public sealed class EmailApplicationFactory : WebApplicationFactory<Program> {
    private readonly PostgreSqlContainer _dbContainer;
    private Func<EmailDatabase, Task>? _seedDbAsyncFunc = null;

    public EmailApplicationFactory(EmailApplicationDatabaseProvider dbProvider) : base() {
        _dbContainer = dbProvider.DbContainer;
        Database = new EmailDatabase(_dbContainer.GetConnectionString());
    }

    public FakeEmailClient EmailClient { get; private set; } = null!;
    public TestEmailLogger Logger { get; } = new TestEmailLogger();
    public EmailDatabase Database { get; } = null!;

    public void SeedDatabase(Func<EmailDatabase, Task> seed) => _seedDbAsyncFunc = seed;

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
        builder.UseSetting("Email:RetryDelay", "10 milliseconds"); // To speed up the test execution.

        // Now allow the application configuration to be created.
        base.ConfigureWebHost(builder);

        // Then extract the email client from the DI container.
        builder.ConfigureTestServices(services => {
            using var serviceProvider = services.BuildServiceProvider();
            EmailClient = (FakeEmailClient)serviceProvider.GetRequiredService<IEmailClient>();
            EmailClient.Configuration = serviceProvider.GetRequiredService<EmailConfiguration>();
            Database.Configuration = EmailClient.Configuration;
            if (_seedDbAsyncFunc is { }) {
                _seedDbAsyncFunc(Database).Wait(); // Force this to run synchronously. Don't want the test to proceed until this is done.
            }

            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider>(new TestEmailLoggerProvider(Logger))
            );
        });
    }

    #region Database Helpers
    public class EmailDatabase {
        public EmailDatabase(string connectionString) => ConnectionString = connectionString;

        public string ConnectionString { get; }
        public EmailConfiguration Configuration { get; set; } = null!;

        public async Task<EmailEntityDto?> GetEmailByIdAsync(string emailId, bool scrubResult = true) 
            => Guid.TryParse(emailId, out var id) ? await GetEmailByIdAsync(id, scrubResult) : null;
            
        public async Task<EmailEntityDto?> GetEmailByIdAsync(Guid emailId, bool scrubResult = true) {
            await using var connection = await OpenConnectionAsync();
            await using var command = CreateGetEmailByIdCommand(emailId, connection);
            var email = await GetEmailFromCommandAsync(command);
            return scrubResult ? ScrubEmail(email) : email;
        }

        public async Task<EmailEntityDto?> GetEmailWhenReadyAsync(string emailId, TimeSpan? pollingInterval = null, TimeSpan? cancellationTimeout = null) {
            if (!Guid.TryParse(emailId, out var id)) {
                return null;
            }
            pollingInterval ??= TimeSpan.FromMilliseconds(10);

            await using var connection = await OpenConnectionAsync();
            await using var command = CreateGetEmailByIdCommand(id, connection);
            var cancellationToken = new CancellationTokenSource(cancellationTimeout ?? TimeSpan.FromMilliseconds(250)).Token;

            while (true) {
                if (cancellationToken.IsCancellationRequested) {
                    throw new Exception("Email not added to db within the timeout period.");
                }

                try {
                    var email = await GetEmailFromCommandAsync(command);
                    if (email is { }) {
                        return ScrubEmail(email);
                    }
                    await Task.Delay(pollingInterval.Value, cancellationToken);
                } catch (TaskCanceledException tce) {
                    throw new Exception("Email not added to db within the timeout period.", tce);
                }
            }
        }

        public async Task WaitForRetriesAsync(string emailId, TimeSpan? pollingInterval = null, TimeSpan? cancellationTimeout = null) {
            if (!Guid.TryParse(emailId, out var id)) {
                return;
            }
            await WaitForRetriesAsync(id, pollingInterval, cancellationTimeout);
        }
            
        public async Task WaitForRetriesAsync(Guid emailId, TimeSpan? pollingInterval = null, TimeSpan? cancellationTimeout = null) {
            pollingInterval ??= TimeSpan.FromMilliseconds(250);

            await using var connection = await OpenConnectionAsync();
            await using var command = CreateGetEmailByIdCommand(emailId, connection);
            var cancellationToken = new CancellationTokenSource(cancellationTimeout ?? TimeSpan.FromSeconds(10)).Token;

            while (true) {
                if (cancellationToken.IsCancellationRequested) {
                    throw new Exception("Email status not resolved within the timeout period.");
                }

                try {
                    var email = await GetEmailFromCommandAsync(command);
                    if (email is { } && (email.Status == EmailStatus.Delivered || email.Status == EmailStatus.Undeliverable)) {
                        break;
                    }
                    await Task.Delay(pollingInterval.Value, cancellationToken);
                } catch (TaskCanceledException tce) {
                    throw new Exception("Email status not resolved within the timeout period.", tce);
                }
            }
        }

        private async Task<EmailEntityDto?> GetEmailFromCommandAsync(NpgsqlCommand command, CancellationToken cancellationToken = default) {
            EmailEntityDto? email = null;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken)) {
                var data = reader.GetString(0);
                email = EmailEntityDto.FromJson(data);
            }
            return email;
        }

        private NpgsqlCommand CreateGetEmailByIdCommand(Guid emailId, NpgsqlConnection connection) {
            var sqlQuery = """
            SELECT data
            FROM   mt_doc_email
            WHERE  id = @Id
            """;
            var command = new NpgsqlCommand(sqlQuery, connection);
            command.Parameters.AddWithValue("Id", emailId);
            return command;
        }

        private EmailEntityDto? ScrubEmail(EmailEntityDto? email) => email?.ScrubSender(Configuration);

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default) {
            var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
    #endregion
}
