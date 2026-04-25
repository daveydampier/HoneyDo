using FluentAssertions;
using HoneyDo.Tests.Fixtures;

namespace HoneyDo.Tests.Integration;

/// <summary>
/// Verifies that SecurityHeadersMiddleware injects the required hardening headers
/// on every response regardless of route or status code.
/// </summary>
public class SecurityHeadersTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/api/auth/login")]          // 400 (missing body) — error path
    [InlineData("/api/lists")]               // 401 (unauthenticated) — auth short-circuit path
    public async Task Response_AlwaysContains_XFrameOptions(string path)
    {
        var response = await _client.GetAsync(path);

        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle("DENY");
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/lists")]
    public async Task Response_AlwaysContains_XContentTypeOptions(string path)
    {
        var response = await _client.GetAsync(path);

        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle("nosniff");
    }
}
