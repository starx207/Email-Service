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

        // TODO: This custom diff tool isn't working and is just nice-to-have. Don't spend too much time on it.
        //       The purpose is to try to get the diff tool to open in the same window as the original file.
        //       It would need to be added to the constructor here if I could get it working.

        // // Configure the VSCode diff tool to open in the same window as the original file.
        // DiffTools.AddToolBasedOn(
        //     basedOn: DiffTool.VisualStudioCode,
        //     name: "VSCode-Reuse-window",
        //     exePath: "code",
        //     launchArguments: new(
        //         Left: (temp, target) => $"--reuse-window --diff \"{target}\" \"{temp}\"",
        //         Right: (temp, target) => $"--reuse-window --diff \"{temp}\" \"{target}\""
        //     )
        // );
    }
}
