using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Auth;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_ValidRequest_Returns201WithToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "test@example.com",
            password = "Password1!",
            displayName = "Test User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.Token.Should().NotBeNullOrEmpty();
        body.DisplayName.Should().Be("Test User");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var payload = new { email = "dupe@example.com", password = "Password1!", displayName = "User" };
        await _client.PostAsJsonAsync("/api/auth/register", payload);

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "login@example.com",
            password = "Password1!",
            displayName = "Login User"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "login@example.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns404()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "wrongpw@example.com",
            password = "Password1!",
            displayName = "User"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "wrongpw@example.com",
            password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@example.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_InvalidEmailFormat_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email",
            password = "Password1!",
            displayName = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_PasswordTooShort_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "short@example.com",
            password = "abc",
            displayName = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_MissingDisplayName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "nodisplay@example.com",
            password = "Password1!",
            displayName = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private record AuthResponse(string Token, Guid ProfileId, string DisplayName);
}
