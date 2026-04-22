using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Members;

public class MemberTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task GetMembers_Returns200WithOwnerListed()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);

        var response = await client.GetAsync($"/api/lists/{listId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var members = await response.Content.ReadFromJsonAsync<List<MemberResponse>>();
        members.Should().HaveCount(1);
        members![0].Role.Should().Be("Owner");
    }

    [Fact]
    public async Task AddMember_OwnerAddsContributor_Returns200()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient(), displayName: "Member");
        var listId = await TestApi.CreateListAsync(ownerClient);

        var response = await ownerClient.PostAsJsonAsync($"/api/lists/{listId}/members", new
        {
            email = memberEmail,
            role = "Contributor"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MemberResponse>();
        body!.Role.Should().Be("Contributor");
        body.DisplayName.Should().Be("Member");
    }

    [Fact]
    public async Task AddMember_ContributorCannotAddMembers_Returns403()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (memberClient, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, _, thirdEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        var response = await memberClient.PostAsJsonAsync($"/api/lists/{listId}/members", new
        {
            email = thirdEmail,
            role = "Contributor"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddMember_AlreadyMember_Returns400()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        var response = await ownerClient.PostAsJsonAsync($"/api/lists/{listId}/members", new
        {
            email = memberEmail,
            role = "Contributor"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddMember_UnknownEmail_Returns404()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);

        var response = await ownerClient.PostAsJsonAsync($"/api/lists/{listId}/members", new
        {
            email = "nobody@test.com",
            role = "Contributor"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveMember_OwnerRemovesContributor_Returns204()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, memberId, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        var response = await ownerClient.DeleteAsync($"/api/lists/{listId}/members/{memberId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var members = await ownerClient.GetFromJsonAsync<List<MemberResponse>>($"/api/lists/{listId}/members");
        members.Should().HaveCount(1);
    }

    [Fact]
    public async Task RemoveMember_ContributorCannotRemoveMembers_Returns403()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (memberClient, _, memberId, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        var response = await memberClient.DeleteAsync($"/api/lists/{listId}/members/{memberId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveMember_CannotRemoveOwner_Returns403()
    {
        var (ownerClient, _, ownerId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);

        var response = await ownerClient.DeleteAsync($"/api/lists/{listId}/members/{ownerId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateMemberRole_OwnerChangesContributorRole_Returns200()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, memberId, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);
        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        var response = await ownerClient.PatchAsJsonAsync(
            $"/api/lists/{listId}/members/{memberId}", new { role = "Owner" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MemberResponse>();
        body!.Role.Should().Be("Owner");
    }

    [Fact]
    public async Task UpdateMemberRole_CannotDemoteOwner_Returns403()
    {
        var (ownerClient, _, ownerId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);

        var response = await ownerClient.PatchAsJsonAsync(
            $"/api/lists/{listId}/members/{ownerId}", new { role = "Contributor" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMembers_NonMember_Returns404()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (otherClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);

        var response = await otherClient.GetAsync($"/api/lists/{listId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record MemberResponse(Guid ProfileId, string DisplayName, string? AvatarUrl, string Role, DateTime JoinedAt);
}
