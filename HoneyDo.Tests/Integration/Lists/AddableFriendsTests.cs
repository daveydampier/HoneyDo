using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Lists;

public class AddableFriendsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    /// <summary>
    /// Sends a friend request from <paramref name="senderClient"/> to <paramref name="recipientClient"/>
    /// and has the recipient accept it, resulting in an established friendship.
    /// </summary>
    private static async Task MakeFriendsAsync(
        HttpClient senderClient, Guid senderId,
        HttpClient recipientClient, string recipientEmail)
    {
        await senderClient.PostAsJsonAsync("/api/friends", new { email = recipientEmail });
        await recipientClient.PatchAsJsonAsync($"/api/friends/{senderId}", new { accept = true });
    }

    [Fact]
    public async Task GetAddableFriends_FriendNotOnList_ReturnsFriend()
    {
        var (ownerClient, _, ownerId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (friendClient, _, _, friendEmail) = await TestApi.RegisterAsync(factory.CreateClient(), displayName: "Best Friend");
        var listId = await TestApi.CreateListAsync(ownerClient, "My List");

        await MakeFriendsAsync(ownerClient, ownerId, friendClient, friendEmail);

        var response = await ownerClient.GetAsync($"/api/lists/{listId}/addable-friends");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var friends = await response.Content.ReadFromJsonAsync<List<AddableFriendResponse>>();
        friends.Should().ContainSingle(f => f.Email == friendEmail);
        friends![0].DisplayName.Should().Be("Best Friend");
    }

    [Fact]
    public async Task GetAddableFriends_FriendAlreadyOnList_IsExcluded()
    {
        var (ownerClient, _, ownerId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (friendClient, _, _, friendEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Team List");

        await MakeFriendsAsync(ownerClient, ownerId, friendClient, friendEmail);
        await TestApi.AddMemberAsync(ownerClient, listId, friendEmail);

        var response = await ownerClient.GetAsync($"/api/lists/{listId}/addable-friends");

        var friends = await response.Content.ReadFromJsonAsync<List<AddableFriendResponse>>();
        friends.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAddableFriends_PendingFriendRequest_IsExcluded()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, _, pendingEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Pending List");

        // Send a request but don't accept — stays Pending, not an accepted friend
        await ownerClient.PostAsJsonAsync("/api/friends", new { email = pendingEmail });

        var response = await ownerClient.GetAsync($"/api/lists/{listId}/addable-friends");

        var friends = await response.Content.ReadFromJsonAsync<List<AddableFriendResponse>>();
        friends.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAddableFriends_NoFriends_ReturnsEmpty()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Solo List");

        var response = await client.GetAsync($"/api/lists/{listId}/addable-friends");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var friends = await response.Content.ReadFromJsonAsync<List<AddableFriendResponse>>();
        friends.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAddableFriends_FriendshipInitiatedByFriend_StillReturned()
    {
        // Friendship where the OTHER user sent the original request
        var (ownerClient, _, ownerId, ownerEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var (friendClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient(), displayName: "Initiator");
        var listId = await TestApi.CreateListAsync(ownerClient, "Reverse Friend List");

        // Friend sends request TO owner; owner accepts
        await friendClient.PostAsJsonAsync("/api/friends", new { email = ownerEmail });
        // We need the friend's ID to accept — get it from owner's pending received
        var friendsData = await ownerClient.GetFromJsonAsync<FriendsResult>("/api/friends");
        var requesterId = friendsData!.PendingReceived[0].RequesterId;
        await ownerClient.PatchAsJsonAsync($"/api/friends/{requesterId}", new { accept = true });

        var response = await ownerClient.GetAsync($"/api/lists/{listId}/addable-friends");

        var addable = await response.Content.ReadFromJsonAsync<List<AddableFriendResponse>>();
        addable.Should().ContainSingle(f => f.DisplayName == "Initiator");
    }

    [Fact]
    public async Task GetAddableFriends_NonMember_Returns404()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (otherClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient, "Private List");

        var response = await otherClient.GetAsync($"/api/lists/{listId}/addable-friends");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record AddableFriendResponse(Guid ProfileId, string DisplayName, string Email, string? AvatarUrl);
    private record FriendsResult(List<FriendInfo> Friends, List<ReceivedInfo> PendingReceived, List<SentInfo> PendingSent);
    private record FriendInfo(Guid ProfileId, string DisplayName, string Email);
    private record ReceivedInfo(Guid RequesterId, string DisplayName, string Email);
    private record SentInfo(Guid AddresseeId, string DisplayName, string Email);
}
