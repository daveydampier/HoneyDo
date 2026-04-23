using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Lists;

/// <summary>
/// Tests for the richer fields added to TodoListResponse:
/// per-status task counts, contributorNames, and tags.
/// </summary>
public class ListResponseFieldTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // ── Status counts ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLists_NewList_AllStatusCountsAreZero()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Empty List");

        var lists = await client.GetFromJsonAsync<List<ListDetail>>("/api/lists");

        var list = lists!.Single(l => l.Id == listId);
        list.NotStartedCount.Should().Be(0);
        list.PartialCount.Should().Be(0);
        list.CompleteCount.Should().Be(0);
        list.AbandonedCount.Should().Be(0);
    }

    [Fact]
    public async Task GetLists_AfterCreatingItems_NotStartedCountReflectsItemCount()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Count Test");

        await TestApi.CreateItemAsync(client, listId, "Item 1");
        await TestApi.CreateItemAsync(client, listId, "Item 2");
        await TestApi.CreateItemAsync(client, listId, "Item 3");

        var lists = await client.GetFromJsonAsync<List<ListDetail>>("/api/lists");
        var list = lists!.Single(l => l.Id == listId);

        list.NotStartedCount.Should().Be(3);
        list.PartialCount.Should().Be(0);
        list.CompleteCount.Should().Be(0);
        list.AbandonedCount.Should().Be(0);
    }

    [Fact]
    public async Task GetLists_AfterUpdatingItemStatuses_CountsReflectEachStatus()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Status Counts");

        var item1 = await TestApi.CreateItemAsync(client, listId, "Will be partial");
        var item2 = await TestApi.CreateItemAsync(client, listId, "Will be complete");
        var item3 = await TestApi.CreateItemAsync(client, listId, "Will be abandoned");
        await TestApi.CreateItemAsync(client, listId, "Stays not started");

        // Advance statuses: 2=Partial, 3=Complete, 4=Abandoned
        await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{item1}", new { statusId = 2 });
        await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{item2}", new { statusId = 3 });
        await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{item3}", new { statusId = 4 });

        var lists = await client.GetFromJsonAsync<List<ListDetail>>("/api/lists");
        var list = lists!.Single(l => l.Id == listId);

        list.NotStartedCount.Should().Be(1);
        list.PartialCount.Should().Be(1);
        list.CompleteCount.Should().Be(1);
        list.AbandonedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetListById_ReturnsStatusCounts()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Single List Counts");

        var itemId = await TestApi.CreateItemAsync(client, listId, "A task");
        await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{itemId}", new { statusId = 3 });

        var list = await client.GetFromJsonAsync<ListDetail>($"/api/lists/{listId}");

        list!.NotStartedCount.Should().Be(0);
        list.CompleteCount.Should().Be(1);
    }

    // ── Contributor names ────────────────────────────────────────────────────

    [Fact]
    public async Task GetLists_SoloOwner_ContributorNamesIsEmpty()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Solo List");

        var lists = await client.GetFromJsonAsync<List<ListDetail>>("/api/lists");
        var list = lists!.Single(l => l.Id == listId);

        list.ContributorNames.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLists_WithContributors_ContributorNamesContainsTheirDisplayNames()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient(), displayName: "Alice");
        var (_, _, _, jami) = await TestApi.RegisterAsync(factory.CreateClient(), displayName: "Jami");
        var (_, _, _, danny) = await TestApi.RegisterAsync(factory.CreateClient(), displayName: "Danny");

        var listId = await TestApi.CreateListAsync(ownerClient, "Shared List");
        await TestApi.AddMemberAsync(ownerClient, listId, jami);
        await TestApi.AddMemberAsync(ownerClient, listId, danny);

        var lists = await ownerClient.GetFromJsonAsync<List<ListDetail>>("/api/lists");
        var list = lists!.Single(l => l.Id == listId);

        list.ContributorNames.Should().HaveCount(2);
        list.ContributorNames.Should().Contain("Jami");
        list.ContributorNames.Should().Contain("Danny");
    }

    [Fact]
    public async Task GetLists_ContributorViewingList_OwnerNameCorrectAndContributorNamesExcludesOwner()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient(), displayName: "TheOwner");
        var (memberClient, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient(), displayName: "TheMember");

        var listId = await TestApi.CreateListAsync(ownerClient, "Collaborative");
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        var lists = await memberClient.GetFromJsonAsync<List<ListDetail>>("/api/lists");
        var list = lists!.Single(l => l.Id == listId);

        list.OwnerName.Should().Be("TheOwner");
        list.ContributorNames.Should().ContainSingle().Which.Should().Be("TheMember");
    }

    [Fact]
    public async Task GetListById_WithContributors_ContributorNamesPopulated()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient(), displayName: "Owner");
        var (_, _, _, contrib) = await TestApi.RegisterAsync(factory.CreateClient(), displayName: "Contrib");

        var listId = await TestApi.CreateListAsync(ownerClient, "Detail List");
        await TestApi.AddMemberAsync(ownerClient, listId, contrib);

        var list = await ownerClient.GetFromJsonAsync<ListDetail>($"/api/lists/{listId}");

        list!.ContributorNames.Should().ContainSingle().Which.Should().Be("Contrib");
    }

    // ── Tags on list response ────────────────────────────────────────────────

    [Fact]
    public async Task GetLists_NoTaggedItems_TagsIsEmpty()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "No Tags");
        await TestApi.CreateItemAsync(client, listId, "Untagged task");

        var lists = await client.GetFromJsonAsync<List<ListDetail>>("/api/lists");
        var list = lists!.Single(l => l.Id == listId);

        list.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLists_AfterApplyingTag_TagAppearsInListTags()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Tagged List");
        var itemId = await TestApi.CreateItemAsync(client, listId, "A task");
        var tagId = await TestApi.CreateTagAsync(client, "Urgent", "#FF0000");

        await client.PostAsJsonAsync($"/api/lists/{listId}/items/{itemId}/tags/{tagId}", new { });

        var lists = await client.GetFromJsonAsync<List<ListDetail>>("/api/lists");
        var list = lists!.Single(l => l.Id == listId);

        list.Tags.Should().ContainSingle(t => t.Id == tagId && t.Name == "Urgent" && t.Color == "#FF0000");
    }

    [Fact]
    public async Task GetLists_MultipleItemsWithSameTag_TagAppearsOnceInListTags()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Dedup Tags");
        var item1 = await TestApi.CreateItemAsync(client, listId, "Task 1");
        var item2 = await TestApi.CreateItemAsync(client, listId, "Task 2");
        var tagId = await TestApi.CreateTagAsync(client, "Work", "#0000FF");

        await client.PostAsJsonAsync($"/api/lists/{listId}/items/{item1}/tags/{tagId}", new { });
        await client.PostAsJsonAsync($"/api/lists/{listId}/items/{item2}/tags/{tagId}", new { });

        var lists = await client.GetFromJsonAsync<List<ListDetail>>("/api/lists");
        var list = lists!.Single(l => l.Id == listId);

        list.Tags.Should().ContainSingle(t => t.Id == tagId);
    }

    [Fact]
    public async Task GetLists_MultipleDistinctTags_AllTagsReturnedOnList()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Multi Tag");
        var item1 = await TestApi.CreateItemAsync(client, listId, "Task A");
        var item2 = await TestApi.CreateItemAsync(client, listId, "Task B");
        var tagA = await TestApi.CreateTagAsync(client, "Alpha", "#AA0000");
        var tagB = await TestApi.CreateTagAsync(client, "Beta", "#0000AA");

        await client.PostAsJsonAsync($"/api/lists/{listId}/items/{item1}/tags/{tagA}", new { });
        await client.PostAsJsonAsync($"/api/lists/{listId}/items/{item2}/tags/{tagB}", new { });

        var lists = await client.GetFromJsonAsync<List<ListDetail>>("/api/lists");
        var list = lists!.Single(l => l.Id == listId);

        list.Tags.Should().HaveCount(2);
        list.Tags.Should().Contain(t => t.Id == tagA);
        list.Tags.Should().Contain(t => t.Id == tagB);
    }

    // ── Response records ─────────────────────────────────────────────────────

    private record ListDetail(
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
        List<TagSummary> Tags);

    private record TagSummary(Guid Id, string Name, string Color);
}
