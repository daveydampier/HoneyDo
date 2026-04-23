using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Lists;

/// <summary>
/// Tests for GET /api/lists/{listId}/tags (GetListTagsQuery)
/// and the cross-member tag application behaviour in ApplyTagCommand.
/// </summary>
public class ListTagsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // ── GET /api/lists/{listId}/tags ─────────────────────────────────────────

    [Fact]
    public async Task GetListTags_ReturnsOwnerOwnTags()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Owner List");
        await TestApi.CreateTagAsync(ownerClient, "Urgent", "#FF0000");
        await TestApi.CreateTagAsync(ownerClient, "Low", "#00FF00");

        var response = await ownerClient.GetAsync($"/api/lists/{listId}/tags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tags = await response.Content.ReadFromJsonAsync<List<TagResponse>>();
        tags.Should().HaveCount(2);
        tags.Should().Contain(t => t.Name == "Urgent");
        tags.Should().Contain(t => t.Name == "Low");
    }

    [Fact]
    public async Task GetListTags_ContributorSeesAllMemberTags()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (memberClient, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Shared List");
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        await TestApi.CreateTagAsync(ownerClient, "Owner Tag", "#FF0000");
        await TestApi.CreateTagAsync(memberClient, "Member Tag", "#0000FF");

        var response = await memberClient.GetAsync($"/api/lists/{listId}/tags");

        var tags = await response.Content.ReadFromJsonAsync<List<TagResponse>>();
        tags.Should().HaveCount(2);
        tags.Should().Contain(t => t.Name == "Owner Tag");
        tags.Should().Contain(t => t.Name == "Member Tag");
    }

    [Fact]
    public async Task GetListTags_OwnerSeesContributorTags()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (memberClient, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Collab List");
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        await TestApi.CreateTagAsync(memberClient, "Member's Tag", "#AABBCC");

        var response = await ownerClient.GetAsync($"/api/lists/{listId}/tags");

        var tags = await response.Content.ReadFromJsonAsync<List<TagResponse>>();
        tags.Should().ContainSingle(t => t.Name == "Member's Tag");
    }

    [Fact]
    public async Task GetListTags_ExcludesTagsFromNonMembers()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (outsiderClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Private List");

        await TestApi.CreateTagAsync(ownerClient, "Owner Tag", "#FF0000");
        await TestApi.CreateTagAsync(outsiderClient, "Outsider Tag", "#00FF00");

        var tags = await ownerClient.GetFromJsonAsync<List<TagResponse>>($"/api/lists/{listId}/tags");

        tags.Should().ContainSingle(t => t.Name == "Owner Tag");
        tags.Should().NotContain(t => t.Name == "Outsider Tag");
    }

    [Fact]
    public async Task GetListTags_ReturnsEmptyWhenNoMembersHaveTags()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Tagless List");

        var tags = await ownerClient.GetFromJsonAsync<List<TagResponse>>($"/api/lists/{listId}/tags");

        tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetListTags_ReturnedAlphabetically()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Sorted Tags List");
        await TestApi.CreateTagAsync(ownerClient, "Zebra", "#000001");
        await TestApi.CreateTagAsync(ownerClient, "Alpha", "#000002");
        await TestApi.CreateTagAsync(ownerClient, "Middle", "#000003");

        var tags = await ownerClient.GetFromJsonAsync<List<TagResponse>>($"/api/lists/{listId}/tags");

        tags.Should().HaveCount(3);
        tags![0].Name.Should().Be("Alpha");
        tags![1].Name.Should().Be("Middle");
        tags![2].Name.Should().Be("Zebra");
    }

    [Fact]
    public async Task GetListTags_NonMember_Returns404()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (outsiderClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Guarded List");

        var response = await outsiderClient.GetAsync($"/api/lists/{listId}/tags");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetListTags_Unauthenticated_Returns401()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Auth List");

        var response = await factory.CreateClient().GetAsync($"/api/lists/{listId}/tags");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Cross-member ApplyTag ────────────────────────────────────────────────

    [Fact]
    public async Task ApplyTag_ContributorCanApplyOwnerTag_Returns204()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (memberClient, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Shared Work");
        var itemId = await TestApi.CreateItemAsync(ownerClient, listId, "Task");
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        // Tag belongs to the owner, not the contributor
        var ownerTagId = await TestApi.CreateTagAsync(ownerClient, "Owner Tag", "#FF0000");

        var response = await memberClient.PostAsync(
            $"/api/lists/{listId}/items/{itemId}/tags/{ownerTagId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var item = await memberClient.GetFromJsonAsync<ItemResponse>(
            $"/api/lists/{listId}/items/{itemId}");
        item!.Tags.Should().ContainSingle(t => t.Id == ownerTagId);
    }

    [Fact]
    public async Task ApplyTag_OwnerCanApplyContributorTag_Returns204()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (memberClient, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Shared Work 2");
        var itemId = await TestApi.CreateItemAsync(ownerClient, listId, "Task");
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        // Tag belongs to the contributor
        var memberTagId = await TestApi.CreateTagAsync(memberClient, "Member Tag", "#0000FF");

        var response = await ownerClient.PostAsync(
            $"/api/lists/{listId}/items/{itemId}/tags/{memberTagId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ApplyTag_TagFromNonMember_Returns404()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (outsiderClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Protected List");
        var itemId = await TestApi.CreateItemAsync(ownerClient, listId, "Task");

        // Outsider creates a tag but is NOT a member of the list
        var outsiderTagId = await TestApi.CreateTagAsync(outsiderClient, "Outsider Tag", "#FF00FF");

        var response = await ownerClient.PostAsync(
            $"/api/lists/{listId}/items/{itemId}/tags/{outsiderTagId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApplyTag_MemberRemovedFromList_TheirTagsNoLongerApplicable()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (exMemberClient, _, exMemberId, exMemberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Revolving Door List");
        var itemId = await TestApi.CreateItemAsync(ownerClient, listId, "Task");
        await TestApi.AddMemberAsync(ownerClient, listId, exMemberEmail);

        var exMemberTagId = await TestApi.CreateTagAsync(exMemberClient, "Ex-Member Tag", "#CCCCCC");

        // Can apply while still a member
        var applyWhileMember = await ownerClient.PostAsync(
            $"/api/lists/{listId}/items/{itemId}/tags/{exMemberTagId}", null);
        applyWhileMember.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Remove the member
        await ownerClient.DeleteAsync($"/api/lists/{listId}/members/{exMemberId}");

        // Remove the tag first so we can test apply again
        await ownerClient.DeleteAsync($"/api/lists/{listId}/items/{itemId}/tags/{exMemberTagId}");

        // Now their tag is no longer applicable
        var applyAfterRemoval = await ownerClient.PostAsync(
            $"/api/lists/{listId}/items/{itemId}/tags/{exMemberTagId}", null);
        applyAfterRemoval.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record TagResponse(Guid Id, string Name, string Color);
    private record ItemResponse(Guid Id, string Content, List<TagResponse> Tags);
}
