using NSubstitute;
using NSubstitute.ClearExtensions;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace EmailService.Api.Tests;

public class ApiTests : VerifyBase, IClassFixture<EmailApplicationDatabaseProvider> {
    private readonly EmailApplicationFactory _factory;

    public ApiTests(EmailApplicationDatabaseProvider dbProvider) : base() => _factory = new(dbProvider);

    // TODO: Once I implement the async nature of the email client, I would hope for this test to fail.
    //       I'll then need to refactor it to poll the API for the email status.
    [Fact]
    public async Task Post_Endpoint_Sends_Email() {
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

        var savedEmail = await _factory.Database.GetEmailByIdAsync(emailId);
        await Verify(new {
            Sent = _factory.EmailClient.SentEmails.WithSubject("Test Subject 1"),
            Saved = savedEmail,
            Logs = _factory.Logger.LogEntries
        });
    }

    [Fact]
    public async Task Post_Endpoint_Retries_Email_UpTo_Three_Times() {
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
    public async Task Post_Endpoint_StopsRetries_Once_Email_Sends() {
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
            Sent = _factory.EmailClient.SentEmails.WithSubject("Test Subject 3"),
            Saved = savedEmail,
            Logs = _factory.Logger.LogEntries
        });
    }

    [Fact]
    public async Task Post_Endpoint_SetsFinalStatus_WhenEmailSendsOnLastTry() {
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
            Sent = _factory.EmailClient.SentEmails.WithSubject("Test Subject 4"),
            Saved = savedEmail,
            Logs = _factory.Logger.LogEntries
        });
    }
}
