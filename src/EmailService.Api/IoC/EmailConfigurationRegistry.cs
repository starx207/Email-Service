using ServiceRegistryModules;

namespace EmailService.Api.IoC;

public sealed class EmailConfigurationRegistry : AbstractRegistryModule {
    private readonly IConfiguration _configuration;

    public event EventHandler<EmailConfiguration>? Configuring;

    public EmailConfigurationRegistry(IConfiguration configuration) => _configuration = configuration;

    public override void ConfigureServices(IServiceCollection services) {
        var emailConfig = new EmailConfiguration() {
            Host = _configuration.GetValue<string>("Email:Host") ?? string.Empty,
            Port = _configuration.GetValue<int>("Email:Port"),
            SecureSocket = _configuration.GetValue<string>("Email:SecureSocket") ?? string.Empty,
            Sender = _configuration.GetValue<string>("Email:Sender") ?? string.Empty,
            Password = _configuration.GetValue<string>("Email:Password") ?? string.Empty
        };
        Configuring?.Invoke(this, emailConfig);

        services.AddEmailService(emailConfig);
    }
}
