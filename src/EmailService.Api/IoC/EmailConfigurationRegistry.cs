using ServiceRegistryModules;

namespace EmailService.Api.IoC;

public sealed class EmailConfigurationRegistry : AbstractRegistryModule {
    private readonly IConfiguration _configuration;

    public event EventHandler<EmailConfiguration>? Configuring;

    public EmailConfigurationRegistry(IConfiguration configuration) => _configuration = configuration;

    public override void ConfigureServices(IServiceCollection services) {
        var emailConfig = new EmailConfiguration();
        // TODO: Configure the SMTP client from appsettings
        Configuring?.Invoke(this, emailConfig);

        services.AddEmailService(emailConfig);
    }
}
