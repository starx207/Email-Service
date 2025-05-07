using EmailService.Api.Tests.Harness;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace EmailService.Api.Tests;

public class Endpoint_Email_Post : VerifyBase, IClassFixture<EmailApplicationDatabaseProvider> {
    private readonly EmailApplicationFactory _factory;

    public Endpoint_Email_Post(EmailApplicationDatabaseProvider dbProvider) : base() => _factory = new(dbProvider);

    [Fact]
    public async Task Should_Send_Email() {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/email", new {
            To = "some@email.com",
            Subject = "Test Subject 1",
            Body = "Test Body"
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var emailId = await response.Content.ReadAsStringAsync();

        await _factory.Database.WaitForRetriesAsync(emailId);

        var savedEmail = await _factory.Database.GetEmailByIdAsync(emailId);
        await Verify(new {
            Sent = _factory.EmailClient.SentEmails,
            Saved = savedEmail,
            Logs = _factory.Logger.LogEntries
        });
    }

    [Fact]
    public async Task Should_Retry_Email_UpTo_Three_Times() {
        // Arrange
        using var client = _factory.CreateClient();

        // Simulate a failure when trying to send the email.
        _factory.EmailClient.Configure().SendAsync(default!, default).ThrowsAsyncForAnyArgs(new Exception("Simulated failure"));

        // Act
        var response = await client.PostAsJsonAsync("/api/email", new {
            To = "some@email.com",
            Subject = "Test Subject 2",
            Body = "Test Body"
        });

        response.EnsureSuccessStatusCode();
        var emailId = await response.Content.ReadAsStringAsync();

        // Wait for the email retries to complete.
        await _factory.Database.WaitForRetriesAsync(emailId);

        // Assert
        var savedEmail = await _factory.Database.GetEmailByIdAsync(emailId);
        await Verify(new {
            Saved = savedEmail,
            Logs = _factory.Logger.LogEntries
        });
    }

    [Fact]
    public async Task Should_Stop_Retries_Once_Email_Sends() {
        // Arrange
        using var client = _factory.CreateClient();

        // Simulate a failure for the initial attempt and first retry, then succeed.
        _factory.EmailClient.WhenForAnyArgs(x => x.SendAsync(default!, default))
            .Do(Callback.FirstThrow(new Exception("Simulated failure"))
                .ThenThrow(new Exception("Simulated failure")));

        // Act
        var response = await client.PostAsJsonAsync("/api/email", new {
            To = "some@email.com",
            Subject = "Test Subject 3",
            Body = "Test Body"
        });

        response.EnsureSuccessStatusCode();
        var emailId = await response.Content.ReadAsStringAsync();

        // Wait for the email retries to complete.
        await _factory.Database.WaitForRetriesAsync(emailId);

        // Assert
        var savedEmail = await _factory.Database.GetEmailByIdAsync(emailId);
        await Verify(new {
            Sent = _factory.EmailClient.SentEmails,
            Saved = savedEmail,
            Logs = _factory.Logger.LogEntries
        });
    }

    [Fact]
    public async Task Should_SetFinalStatus_WhenEmailSendsOnLastTry() {
        // Arrange
        using var client = _factory.CreateClient();

        // Simulate a failure for the initial attempt and first two retries, then succeed.
        _factory.EmailClient.WhenForAnyArgs(x => x.SendAsync(default!, default))
            .Do(Callback.FirstThrow(new Exception("Simulated failure"))
                .ThenThrow(new Exception("Simulated failure"))
                .ThenThrow(new Exception("Simulated failure")));

        // Act
        var response = await client.PostAsJsonAsync("/api/email", new {
            To = "some@email.com",
            Subject = "Test Subject 4",
            Body = "Test Body"
        });

        response.EnsureSuccessStatusCode();
        var emailId = await response.Content.ReadAsStringAsync();

        // Wait for the email retries to complete.
        await _factory.Database.WaitForRetriesAsync(emailId);

        // Assert
        var savedEmail = await _factory.Database.GetEmailByIdAsync(emailId);
        await Verify(new {
            Sent = _factory.EmailClient.SentEmails,
            Saved = savedEmail,
            Logs = _factory.Logger.LogEntries
        });
    }

    [Fact]
    public async Task ShouldReturn_WithoutWaitingForEmailToSend() {
        // Arrange
        using var client = _factory.CreateClient();

        // Simulate an email taking a long time to send
        _factory.EmailClient.Configure().SendAsync(default!, default).ReturnsForAnyArgs(async callInfo => {
            await Task.Delay(TimeSpan.FromMinutes(1));
            await _factory.EmailClient.SendInternalAsync(callInfo);
        });

        // Act
        var response = Should.CompleteIn(async () => await client.PostAsJsonAsync("/api/email", new {
            To = "some@email.com",
            Subject = "Test Subject",
            Body = "Test Body"
        }), TimeSpan.FromSeconds(5));

        response.EnsureSuccessStatusCode();
        var emailId = await response.Content.ReadAsStringAsync();
        var pendingEmail = await _factory.Database.GetEmailWhenReadyAsync(emailId);

        // Assert
        await Verify(new {
            EmailId = emailId,
            Sent = _factory.EmailClient.SentEmails,
            Saved = pendingEmail,
            Logs = _factory.Logger.LogEntries
        });
    }
}
