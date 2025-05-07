using EmailService.Api.Tests.Harness;
using NSubstitute;

// NOTE: There is a bug in ServiceRegistryModules where a configured event handler cannot be
//       resolved by its fully-qualified name if it is not in the root assembly namespace.
//       So it is important to keep this class in the root namespace so it is called correctly.
namespace EmailService.Api.Tests;

public static class ServiceRegistryOverrides {
    public static void OnConfiguringEmailClient(object sender, EmailConfiguration e)
        => e.CustomClientOverride = Substitute.ForPartsOf<FakeEmailClient>();
}
