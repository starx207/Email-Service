using EmailService.Api.Tests.Harness;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using Shouldly;
using System.Dynamic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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

    [Fact]
    public async Task ShouldNotBlock_SubsequentEmails_WhenOneIsSlowToSend() {
        // Arrange
        using var client = _factory.CreateClient();
        var subject1 = "Test Subject 1";
        var subject2 = "Test Subject 2";

        // Simulate the first email taking a long time to send
        _factory.EmailClient.Configure().SendAsync(default!, default).ReturnsForAnyArgs(async callInfo => {
            var sub = callInfo.Arg<MimeKit.MimeMessage>().Subject;
            if (sub == subject1) {
                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }
            await _factory.EmailClient.SendInternalAsync(callInfo);
        });

        // Act
        var response1 = await client.PostAsJsonAsync("/api/email", new {
            To = "some@email.com",
            Subject = subject1,
            Body = "Test Body"
        });
        var response2 = await client.PostAsJsonAsync("/api/email", new {
            To = "some@email.com",
            Subject = subject2,
            Body = "Test Body"
        });

        // Wait for both emails to be sent, then retrieve them
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();
        var emailId1 = await response1.Content.ReadAsStringAsync();
        var emailId2 = await response2.Content.ReadAsStringAsync();

        await _factory.Database.WaitForRetriesAsync(emailId1);
        await _factory.Database.WaitForRetriesAsync(emailId2);

        var email1 = await _factory.Database.GetEmailByIdAsync(emailId1);
        var email2 = await _factory.Database.GetEmailByIdAsync(emailId2);

        // Assert
        email1!.SubmittedOn.ShouldBeLessThan(email2!.SubmittedOn);
        email1.DeliveryDate!.Value
            .ShouldBeGreaterThan(email2.DeliveryDate!.Value);
    }

    [Fact]
    public async Task Should_Return_Error_Details_When_Request_Invalid() {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/email", new {
            To = "invalid-email",
            Subject = "",
            Body = ""
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = await response.Content.ReadFromJsonAsync<ExpectedValidationErr[]>();

        await Verify(errors);
    }

    public class ExpectedValidationErr {
        public string Field { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Problem { get; set; } = string.Empty;
    }
}
