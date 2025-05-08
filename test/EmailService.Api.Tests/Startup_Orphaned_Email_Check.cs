using System;
using EmailService.Api.Tests.Harness;

namespace EmailService.Api.Tests;

public class Startup_Orphaned_Email_Check : VerifyBase, IClassFixture<EmailApplicationDatabaseProvider> {
    private readonly EmailApplicationFactory _factory;

    public Startup_Orphaned_Email_Check(EmailApplicationDatabaseProvider dbProvider) : base() => _factory = new(dbProvider);

    [Fact]
    public async Task Should_Identify_Failed_Or_Pending_Emails_And_Resubmit_Them() {
        // Arrange
        var pendingEmailId = Guid.Empty;
        var failedOnFirstAttemptId = Guid.Empty;
        var failedOnSecondAttemptId = Guid.Empty;
        var failedOnThirdAttemptId = Guid.Empty;
        var now = DateTime.UtcNow;

        _factory.SeedDatabase(async db => {
            // Seed the database with 4 email records that were left in a failed or orphaned state.
            pendingEmailId = await db.CreateOrphanedEmailAsync(options => {
                options.Subject = "Orphaned Pending";
                options.Submitted = now.AddHours(-2);
                options.SendAttempt = 0;
                options.LastAttempted = null;
            });

            failedOnFirstAttemptId = await db.CreateOrphanedEmailAsync(options => {
                options.Subject = "Orphaned after 1";
                options.Submitted = now.AddHours(-2);
                options.SendAttempt = 1;
                options.LastAttempted = options.Submitted.AddSeconds(30);
            });

            failedOnSecondAttemptId = await db.CreateOrphanedEmailAsync(options => {
                options.Subject = "Orphaned after 2";
                options.Submitted = now.AddHours(-2);
                options.SendAttempt = 2;
                options.LastAttempted = options.Submitted.AddSeconds(60);
            });

            failedOnThirdAttemptId = await db.CreateOrphanedEmailAsync(options => {
                options.Subject = "Orphaned after 3";
                options.Submitted = now.AddHours(-2);
                options.SendAttempt = 3;
                options.LastAttempted = options.Submitted.AddSeconds(120);
            });
        });

        // Act
        // Simulate the application startup process, which should check for orphaned emails and resubmit them.
        using var client = _factory.CreateClient();

        await Task.WhenAll(
            _factory.Database.WaitForRetriesAsync(pendingEmailId),
            _factory.Database.WaitForRetriesAsync(failedOnFirstAttemptId),
            _factory.Database.WaitForRetriesAsync(failedOnSecondAttemptId),
            _factory.Database.WaitForRetriesAsync(failedOnThirdAttemptId)
        );

        var pendingEmail = await _factory.Database.GetEmailByIdAsync(pendingEmailId);
        var failedFirstEmail = await _factory.Database.GetEmailByIdAsync(failedOnFirstAttemptId);
        var failedSecondEmail = await _factory.Database.GetEmailByIdAsync(failedOnSecondAttemptId);
        var failedThirdEmail = await _factory.Database.GetEmailByIdAsync(failedOnThirdAttemptId);

        // Assert
        await Verify(new {
            // Order the emails by subject for consistent output
            Sent = _factory.EmailClient.SentEmails.OrderBy(e => e.Subject).ToList(),
            Saved = new[] {pendingEmail, failedFirstEmail, failedSecondEmail, failedThirdEmail},
            Logs = _factory.Logger.LogEntries
        });
    }
}
