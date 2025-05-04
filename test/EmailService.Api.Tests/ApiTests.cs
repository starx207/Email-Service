using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace EmailService.Api.Tests;

// TODO: Not sure I want to use IClassFixture here, but it is a good start
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
        await Verify(_factory.EmailClient.SentEmails);
    }
}