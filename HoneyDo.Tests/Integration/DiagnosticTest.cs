using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration;

public class DiagnosticTest(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Register_SurfaceActualErrorIfAny()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "diag@example.com",
            password = "Password1!",
            displayName = "Diag User"
        });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"Response body: {body}");
    }
}
