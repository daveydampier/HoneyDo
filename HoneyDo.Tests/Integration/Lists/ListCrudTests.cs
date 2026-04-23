using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Lists;

public class ListCrudTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task GetLists_ReturnsOwnedAndContributedLists()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (memberClient, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());

        var listId = await TestApi.CreateListAsync(ownerClient, "Owner List");
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        var response = await memberClient.GetAsync("/api/lists");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var lists = await response.Content.ReadFromJsonAsync<List<ListResponse>>();
        lists.Should().ContainSingle(l => l.Id == listId);
        lists!.Single(l => l.Id == listId).Role.Should().Be("Contributor");
    }

    [Fact]
    public async Task GetListById_Returns200()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "My List");

        var response = await client.GetAsync($"/api/lists/{listId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Id.Should().Be(listId);
        body.Title.Should().Be("My List");
        body.Role.Should().Be("Owner");
    }

    [Fact]
    public async Task CreateList_Returns201WithLocationHeader()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PostAsJsonAsync("/api/lists", new { title = "New List" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Title.Should().Be("New List");
        body.Role.Should().Be("Owner");
        body.MemberCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateList_EmptyTitle_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PostAsJsonAsync("/api/lists", new { title = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateList_OwnerCanRename_Returns200()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Old Title");

        var response = await client.PatchAsJsonAsync($"/api/lists/{listId}", new { title = "New Title" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task UpdateList_ContributorCannotRename_Returns403()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (memberClient, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Original");
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        var response = await memberClient.PatchAsJsonAsync($"/api/lists/{listId}", new { title = "Hijacked" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteList_OwnerCanDelete_Returns204()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "To Delete");

        var response = await client.DeleteAsync($"/api/lists/{listId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/lists/{listId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteList_ContributorCannotDelete_Returns403()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (memberClient, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Protected");
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        var response = await memberClient.DeleteAsync($"/api/lists/{listId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateList_DuplicateTitleForSameOwner_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        await TestApi.CreateListAsync(client, "Unique Title");

        var response = await client.PostAsJsonAsync("/api/lists", new { title = "Unique Title" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private record ListResponse(
        Guid Id,
        string Title,
        string Role,
        string OwnerName,
        List<string> ContributorNames,
        int MemberCount,
        int NotStartedCount,
        int PartialCount,
        int CompleteCount,
        int AbandonedCount,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? ClosedAt,
        List<TagSummary> Tags);

    private record TagSummary(Guid Id, string Name, string Color);
}
