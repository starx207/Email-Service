namespace EmailService.Api.Tests.Configuration;

public class VerifyTests {
    [Fact]
    public Task VerificationSettings() => VerifyChecks.Run();
}
