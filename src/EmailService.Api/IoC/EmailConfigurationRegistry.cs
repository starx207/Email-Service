using Npgsql;
using ServiceRegistryModules;

namespace EmailService.Api.IoC;

public sealed class EmailConfigurationRegistry : AbstractRegistryModule {
    private readonly IConfiguration _configuration;

    public event EventHandler<EmailConfiguration>? Configuring;

    public string? ConnectionString { get; set; }

    public EmailConfigurationRegistry(IConfiguration configuration) => _configuration = configuration;

    public override void ConfigureServices(IServiceCollection services) {
        string connectionString;
        if (ConnectionString is { Length: > 0 }) {
            connectionString = ConnectionString;
        } else {
            // I've split up the core connection string from the credentials for 2 reasons:
            // 1. It allows me to define credentials in environment variables to keep them out of source control.
            // 2. Since they're in environment variables, I can use them both here and in the docker-compose file when configuring postgres.
            var connectionBuilder = new NpgsqlConnectionStringBuilder(_configuration.GetConnectionString("Default") ?? string.Empty);
            if (_configuration.GetConnectionString("User") is { Length: > 0 } user) {
                connectionBuilder.Username = user;
            }
            if (_configuration.GetConnectionString("Password") is { Length: > 0 } password) {
                connectionBuilder.Password = password;
            }
            connectionString = connectionBuilder.ToString();
        }

        var emailConfig = new EmailConfiguration() {
            Host = _configuration.GetValue<string>("Email:Host") ?? string.Empty,
            Port = _configuration.GetValue<int>("Email:Port"),
            SecureSocket = _configuration.GetValue<string>("Email:SecureSocket") ?? string.Empty,
            Sender = _configuration.GetValue<string>("Email:Sender") ?? string.Empty,
            Password = _configuration.GetValue<string>("Email:Password") ?? string.Empty,
            RetryDelay = _configuration.GetValue<string>("Email:RetryDelay"),
            StoreConnectionString = connectionString
        };
        Configuring?.Invoke(this, emailConfig);

        services.AddEmailService(emailConfig);
    }
}
