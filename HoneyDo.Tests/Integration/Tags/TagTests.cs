using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Tags;

public class TagTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task CreateTag_ValidData_Returns200()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PostAsJsonAsync("/api/tags", new { name = "Urgent", color = "#FF0000" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TagResponse>();
        body!.Name.Should().Be("Urgent");
        body.Color.Should().Be("#FF0000");
    }

    [Fact]
    public async Task CreateTag_InvalidHexColor_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PostAsJsonAsync("/api/tags", new { name = "Tag", color = "red" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_EmptyName_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PostAsJsonAsync("/api/tags", new { name = "", color = "#FF0000" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTags_ReturnsOwnedTags()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        await TestApi.CreateTagAsync(client, "Work", "#0000FF");
        await TestApi.CreateTagAsync(client, "Personal", "#00FF00");

        var response = await client.GetAsync("/api/tags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tags = await response.Content.ReadFromJsonAsync<List<TagResponse>>();
        tags.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTags_DoesNotReturnOtherUsersTags()
    {
        var (client1, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (client2, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        await TestApi.CreateTagAsync(client1, "Private Tag", "#FF0000");

        var response = await client2.GetAsync("/api/tags");

        var tags = await response.Content.ReadFromJsonAsync<List<TagResponse>>();
        tags.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteTag_Returns204()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var tagId = await TestApi.CreateTagAsync(client);

        var response = await client.DeleteAsync($"/api/tags/{tagId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ApplyTag_ToItem_Returns204()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId);
        var tagId = await TestApi.CreateTagAsync(client);

        var response = await client.PostAsync($"/api/lists/{listId}/items/{itemId}/tags/{tagId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var item = await client.GetFromJsonAsync<ItemResponse>($"/api/lists/{listId}/items/{itemId}");
        item!.Tags.Should().ContainSingle(t => t.Id == tagId);
    }

    [Fact]
    public async Task ApplyTag_IdempotentWhenAppliedTwice()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId);
        var tagId = await TestApi.CreateTagAsync(client);

        await client.PostAsync($"/api/lists/{listId}/items/{itemId}/tags/{tagId}", null);
        var response = await client.PostAsync($"/api/lists/{listId}/items/{itemId}/tags/{tagId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var item = await client.GetFromJsonAsync<ItemResponse>($"/api/lists/{listId}/items/{itemId}");
        item!.Tags.Should().HaveCount(1);
    }

    [Fact]
    public async Task ApplyTag_OtherUsersTag_Returns404()
    {
        var (client1, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (client2, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client1);
        var itemId = await TestApi.CreateItemAsync(client1, listId);
        var otherUserTagId = await TestApi.CreateTagAsync(client2);

        var response = await client1.PostAsync($"/api/lists/{listId}/items/{itemId}/tags/{otherUserTagId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveTag_FromItem_Returns204AndTagIsGone()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId);
        var tagId = await TestApi.CreateTagAsync(client);
        await client.PostAsync($"/api/lists/{listId}/items/{itemId}/tags/{tagId}", null);

        var response = await client.DeleteAsync($"/api/lists/{listId}/items/{itemId}/tags/{tagId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var item = await client.GetFromJsonAsync<ItemResponse>($"/api/lists/{listId}/items/{itemId}");
        item!.Tags.Should().BeEmpty();
    }

    private record TagResponse(Guid Id, string Name, string Color);
    private record ItemResponse(Guid Id, string Content, List<TagResponse> Tags);
}
