using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Lists;

public class CloseListTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // Helper: mark an item Complete via the API
    private static async Task CompleteItemAsync(HttpClient client, Guid listId, Guid itemId)
    {
        var res = await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{itemId}", new { statusId = 3 });
        res.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CloseList_AllItemsComplete_Returns200WithClosedAt()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Shopping");
        var itemId = await TestApi.CreateItemAsync(client, listId, "Buy milk");
        await CompleteItemAsync(client, listId, itemId);

        var response = await client.PostAsync($"/api/lists/{listId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CloseList_AllItemsPartial_Returns200()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Partial List");
        var itemId = await TestApi.CreateItemAsync(client, listId, "Started item");
        // StatusId 2 = Partial
        var res = await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{itemId}", new { statusId = 2 });
        res.EnsureSuccessStatusCode();

        var response = await client.PostAsync($"/api/lists/{listId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CloseList_AllItemsAbandoned_Returns200()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Abandoned List");
        var itemId = await TestApi.CreateItemAsync(client, listId, "Abandoned item");
        // StatusId 4 = Abandoned
        var res = await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{itemId}", new { statusId = 4 });
        res.EnsureSuccessStatusCode();

        var response = await client.PostAsync($"/api/lists/{listId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CloseList_HasNotStartedItem_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Incomplete List");
        await TestApi.CreateItemAsync(client, listId, "Not started yet");

        var response = await client.PostAsync($"/api/lists/{listId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CloseList_NoItems_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Empty List");

        var response = await client.PostAsync($"/api/lists/{listId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CloseList_ContributorCannotClose_Returns403()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (memberClient, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Guarded List");
        var itemId = await TestApi.CreateItemAsync(ownerClient, listId, "Task");
        await CompleteItemAsync(ownerClient, listId, itemId);
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        var response = await memberClient.PostAsync($"/api/lists/{listId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CloseList_AlreadyClosed_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Close Me Twice");
        var itemId = await TestApi.CreateItemAsync(client, listId, "Task");
        await CompleteItemAsync(client, listId, itemId);

        await client.PostAsync($"/api/lists/{listId}/close", null);
        var response = await client.PostAsync($"/api/lists/{listId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CloseList_Unauthenticated_Returns401()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Auth Test List");

        var response = await factory.CreateClient().PostAsync($"/api/lists/{listId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record ListResponse(Guid Id, string Title, string Role, string OwnerName, int MemberCount, int ItemCount, DateTime? ClosedAt);
}
