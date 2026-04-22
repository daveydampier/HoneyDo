using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Helpers;

/// <summary>
/// Shared test helpers for registering users and seeding common resources.
/// </summary>
public static class TestApi
{
    public static async Task<(HttpClient Client, string Token, Guid ProfileId, string Email)> RegisterAsync(
        HttpClient client, string? email = null, string displayName = "Test User")
    {
        email ??= $"u{Guid.NewGuid():N}@test.com";
        var res = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password1!",
            displayName
        });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<AuthBody>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return (client, body.Token, body.ProfileId, email);
    }

    public static async Task<Guid> CreateListAsync(HttpClient client, string title = "Test List")
    {
        var res = await client.PostAsJsonAsync("/api/lists", new { title });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<IdBody>();
        return body!.Id;
    }

    public static async Task<Guid> CreateItemAsync(HttpClient client, Guid listId, string content = "Test item")
    {
        var res = await client.PostAsJsonAsync($"/api/lists/{listId}/items", new { content });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<IdBody>();
        return body!.Id;
    }

    public static async Task<Guid> CreateTagAsync(HttpClient client, string name = "Tag", string color = "#FF5733")
    {
        var res = await client.PostAsJsonAsync("/api/tags", new { name, color });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<IdBody>();
        return body!.Id;
    }

    public static async Task AddMemberAsync(HttpClient ownerClient, Guid listId, string memberEmail, string role = "Contributor")
    {
        var res = await ownerClient.PostAsJsonAsync($"/api/lists/{listId}/members", new { email = memberEmail, role });
        res.EnsureSuccessStatusCode();
    }

    private record AuthBody(string Token, Guid ProfileId, string DisplayName);
    private record IdBody(Guid Id);
}
