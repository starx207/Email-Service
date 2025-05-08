using EmailService.Api.Tests.Harness;
using Xunit.Abstractions;
using Xunit.Sdk;


[assembly: TestFramework("EmailService.Api.Tests.Configuration.AssemblyFixture", "EmailService.Api.Tests")]
namespace EmailService.Api.Tests.Configuration;
public sealed class AssemblyFixture : XunitTestFramework {
    public AssemblyFixture(IMessageSink messageSink) : base(messageSink) {
        // Stack traces change as the code is refactored - we don't want that to cause test failures.
        VerifierSettings.ScrubMember<Exception>(e => e.StackTrace);
        VerifierSettings.IgnoreMember<EmailEntityDto>(x => x.Status);
    }
}
