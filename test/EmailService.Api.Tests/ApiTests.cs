using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace EmailService.Api.Tests;

public class ApiTests : VerifyBase, IClassFixture<EmailApplicationFactory> {
    private readonly EmailApplicationFactory _factory;

    public ApiTests(EmailApplicationFactory factory) : base() => _factory = factory;

    // TODO: Once I implement the async nature of the email client, I would hope for this test to fail.
    //       I'll then need to refactor it to poll the API for the email status.
    [Fact]
    public async Task Post_Endpoint_Sends_Email() {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/email", new {
            To = "some@email.com",
            Subject = "Test Subject",
            Body = "Test Body"
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var emailId = await response.Content.ReadAsStringAsync();

        var savedEmail = await _factory.Database.GetEmailByIdAsync(emailId);
        await Verify(new {
            Sent = _factory.EmailClient.SentEmails,
            Saved = savedEmail
        });
    }

    [Fact]
    public async Task Post_Endpoint_Retries_Email_UpTo_Three_Times() {
        // Arrange
        using var client = _factory.CreateClient();

        // Simulate a failure when trying to send the email.
        _factory.EmailClient.Configure().SendAsync(default!, default).ThrowsAsyncForAnyArgs<Exception>();

        // Act
        var response = await client.PostAsJsonAsync("/api/email", new {
            To = "some@email.com",
            Subject = "Test Subject",
            Body = "Test Body"
        });

        response.EnsureSuccessStatusCode();
        var emailId = await response.Content.ReadAsStringAsync();

        // Wait for the email retries to complete. But bail out if not done after 5 seconds.
        await _factory.Database.WaitForRetriesAsync(emailId,
            cancellationToken: new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);

        // Assert
        var savedEmail = await _factory.Database.GetEmailByIdAsync(emailId);
        await Verify(savedEmail);
    }
}
