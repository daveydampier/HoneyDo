using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Members;

/// <summary>
/// Tests for POST /api/lists/{listId}/members/{profileId} (AddMemberByIdCommand).
/// This endpoint is used when adding a friend to a list by their profile ID,
/// as opposed to adding by email address.
/// </summary>
public class AddMemberByIdTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task AddMemberById_OwnerAddsUser_Returns200AsContributor()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, inviteeId, _) = await TestApi.RegisterAsync(factory.CreateClient(), displayName: "Friend User");
        var listId = await TestApi.CreateListAsync(ownerClient, "Team List");

        var response = await ownerClient.PostAsync($"/api/lists/{listId}/members/{inviteeId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MemberResponse>();
        body!.Role.Should().Be("Contributor");
        body.DisplayName.Should().Be("Friend User");
    }

    [Fact]
    public async Task AddMemberById_NewMemberAppearsInMemberList()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, inviteeId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);

        await ownerClient.PostAsync($"/api/lists/{listId}/members/{inviteeId}", null);

        var members = await ownerClient.GetFromJsonAsync<List<MemberResponse>>($"/api/lists/{listId}/members");
        members.Should().HaveCount(2);
        members.Should().Contain(m => m.ProfileId == inviteeId && m.Role == "Contributor");
    }

    [Fact]
    public async Task AddMemberById_ContributorCannotAdd_Returns403()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (memberClient, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, thirdId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        var response = await memberClient.PostAsync($"/api/lists/{listId}/members/{thirdId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddMemberById_AlreadyMember_Returns400()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, inviteeId, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        var response = await ownerClient.PostAsync($"/api/lists/{listId}/members/{inviteeId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddMemberById_UnknownProfileId_Returns404()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);
        var nonExistentId = Guid.NewGuid();

        var response = await ownerClient.PostAsync($"/api/lists/{listId}/members/{nonExistentId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddMemberById_NonOwnerList_Returns404()
    {
        // Owner of the list is not the caller
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (otherClient, _, otherId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, thirdId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);

        // Other user (not a member) tries to add someone
        var response = await otherClient.PostAsync($"/api/lists/{listId}/members/{thirdId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record MemberResponse(Guid ProfileId, string DisplayName, string? AvatarUrl, string Role, DateTime JoinedAt);
}
