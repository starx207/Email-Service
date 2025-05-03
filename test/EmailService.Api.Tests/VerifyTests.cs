namespace EmailService.Api.Tests;

public class VerifyTests {
    [Fact]
    public Task VerificationSettings() => VerifyChecks.Run();
}
