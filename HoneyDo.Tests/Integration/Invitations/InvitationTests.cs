using FluentAssertions;
using HoneyDo.Data;
using HoneyDo.Domain;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Invitations;

public class InvitationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    /// <summary>
    /// Seeds an invitation record directly in the database, bypassing the email-send path.
    /// Returns the URL-safe token that can be passed to POST /api/invitations/accept.
    /// </summary>
    private async Task<string> SeedInvitationAsync(Guid inviterId, string toEmail)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Invitations.Add(new Invitation
        {
            Id = Guid.NewGuid(),
            InviterId = inviterId,
            Email = toEmail,
            Token = token,
            SentAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return token;
    }

    [Fact]
    public async Task AcceptInvitation_ValidToken_Returns204()
    {
        var (inviterClient, _, inviterId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (acceptorClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var token = await SeedInvitationAsync(inviterId, "newuser@example.com");

        var response = await acceptorClient.PostAsJsonAsync("/api/invitations/accept", new { token });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AcceptInvitation_ValidToken_CreatesPendingFriendRequest()
    {
        var (inviterClient, _, inviterId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (acceptorClient, _, acceptorId, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var token = await SeedInvitationAsync(inviterId, "someone@example.com");
        await acceptorClient.PostAsJsonAsync("/api/invitations/accept", new { token });

        // The inviter should now have a pending friend request sent to the acceptor
        var inviterFriends = await inviterClient.GetFromJsonAsync<FriendsResult>("/api/friends");
        inviterFriends!.PendingSent.Should().ContainSingle(s => s.AddresseeId == acceptorId);
    }

    [Fact]
    public async Task AcceptInvitation_InvalidToken_Returns404()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PostAsJsonAsync("/api/invitations/accept", new { token = "totally-fake-token" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcceptInvitation_AlreadyAcceptedToken_Returns404()
    {
        var (inviterClient, _, inviterId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (acceptorClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var token = await SeedInvitationAsync(inviterId, "used@example.com");

        // Accept once
        await acceptorClient.PostAsJsonAsync("/api/invitations/accept", new { token });
        // Try to accept again
        var response = await acceptorClient.PostAsJsonAsync("/api/invitations/accept", new { token });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcceptInvitation_InviterAcceptsOwnLink_Returns204WithoutDuplicateFriendRequest()
    {
        // Edge case: the same user who sent the invite registers via the link themselves
        var (inviterClient, _, inviterId, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var token = await SeedInvitationAsync(inviterId, "self@example.com");

        // Inviter accepts their own invitation token
        var response = await inviterClient.PostAsJsonAsync("/api/invitations/accept", new { token });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // No friend request should be created (can't befriend yourself)
        var friends = await inviterClient.GetFromJsonAsync<FriendsResult>("/api/friends");
        friends!.PendingSent.Should().BeEmpty();
        friends.Friends.Should().BeEmpty();
    }

    [Fact]
    public async Task AcceptInvitation_UsersAlreadyFriends_Returns204WithoutDuplicateRequest()
    {
        var (inviterClient, _, inviterId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (acceptorClient, _, acceptorId, acceptorEmail) = await TestApi.RegisterAsync(factory.CreateClient());

        // Establish friendship through the normal friend-request flow
        await inviterClient.PostAsJsonAsync("/api/friends", new { email = acceptorEmail });
        await acceptorClient.PatchAsJsonAsync($"/api/friends/{inviterId}", new { accept = true });

        // Now acceptor uses an invite link from the same inviter
        var token = await SeedInvitationAsync(inviterId, "already-friends@example.com");
        var response = await acceptorClient.PostAsJsonAsync("/api/invitations/accept", new { token });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Friendship count unchanged
        var friends = await acceptorClient.GetFromJsonAsync<FriendsResult>("/api/friends");
        friends!.Friends.Should().HaveCount(1);
    }

    [Fact]
    public async Task AcceptInvitation_Unauthenticated_Returns401()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/invitations/accept", new { token = "any-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record FriendsResult(List<FriendInfo> Friends, List<SentInfo> PendingSent, List<ReceivedInfo> PendingReceived);
    private record FriendInfo(Guid ProfileId, string DisplayName, string Email);
    private record SentInfo(Guid AddresseeId, string DisplayName, string Email);
    private record ReceivedInfo(Guid RequesterId, string DisplayName, string Email);
}
