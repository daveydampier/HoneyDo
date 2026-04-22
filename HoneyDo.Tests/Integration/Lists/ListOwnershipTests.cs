using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Lists;

public class ListOwnershipTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> RegisterAndGetToken(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password1!",
            displayName = "User"
        });
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    [Fact]
    public async Task GetLists_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/lists");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetList_OtherUsersList_Returns404()
    {
        var ownerToken = await RegisterAndGetToken("owner@example.com");
        var otherToken = await RegisterAndGetToken("other@example.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var createResponse = await _client.PostAsJsonAsync("/api/lists", new { title = "My Private List" });
        var list = await createResponse.Content.ReadFromJsonAsync<ListResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        var response = await _client.GetAsync($"/api/lists/{list!.Id}");

        // Must return 404, not 403 — prevents ID enumeration
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateList_DuplicateTitle_Returns400()
    {
        var token = await RegisterAndGetToken("titles@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/lists", new { title = "Unique Title" });
        var response = await _client.PostAsJsonAsync("/api/lists", new { title = "Unique Title" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private record AuthResponse(string Token, Guid ProfileId, string DisplayName);
    private record ListResponse(Guid Id, string Title);
}
