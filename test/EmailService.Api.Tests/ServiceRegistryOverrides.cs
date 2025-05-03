using NSubstitute;

namespace EmailService.Api.Tests;

public static class ServiceRegistryOverrides {
    public static void OnConfiguringEmailClient(object sender, EmailConfiguration e)
        => e.CustomClientOverride = Substitute.ForPartsOf<FakeEmailClient>();
}
