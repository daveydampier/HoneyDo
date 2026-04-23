using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Friends;

public class FriendTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task GetFriends_NewUser_ReturnsEmptyState()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.GetAsync("/api/friends");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FriendsResult>();
        body!.Friends.Should().BeEmpty();
        body.PendingReceived.Should().BeEmpty();
        body.PendingSent.Should().BeEmpty();
    }

    [Fact]
    public async Task SendFriendRequest_ValidEmail_AppearsInPendingSent()
    {
        var (senderClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, _, recipientEmail) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await senderClient.PostAsJsonAsync("/api/friends", new { email = recipientEmail });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var friends = await senderClient.GetFromJsonAsync<FriendsResult>("/api/friends");
        friends!.PendingSent.Should().HaveCount(1);
        friends.PendingSent[0].AddresseeId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SendFriendRequest_AppearsInRecipientsPendingReceived()
    {
        var (senderClient, _, _, senderEmail) = await TestApi.RegisterAsync(factory.CreateClient());
        var (recipientClient, _, _, recipientEmail) = await TestApi.RegisterAsync(factory.CreateClient());

        await senderClient.PostAsJsonAsync("/api/friends", new { email = recipientEmail });

        var friends = await recipientClient.GetFromJsonAsync<FriendsResult>("/api/friends");
        friends!.PendingReceived.Should().HaveCount(1);
        friends.PendingReceived[0].Email.Should().Be(senderEmail);
    }

    [Fact]
    public async Task SendFriendRequest_InvalidEmail_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PostAsJsonAsync("/api/friends", new { email = "not-an-email" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendFriendRequest_UnknownEmail_SendsInvitationAndReturns200()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PostAsJsonAsync("/api/friends", new { email = "nobody@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SendRequestResult>();
        body!.InvitationSent.Should().BeTrue();
    }

    [Fact]
    public async Task SendFriendRequest_ToSelf_Returns400()
    {
        var (client, _, _, email) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PostAsJsonAsync("/api/friends", new { email });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendFriendRequest_Duplicate_Returns400()
    {
        var (senderClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, _, recipientEmail) = await TestApi.RegisterAsync(factory.CreateClient());

        await senderClient.PostAsJsonAsync("/api/friends", new { email = recipientEmail });
        var response = await senderClient.PostAsJsonAsync("/api/friends", new { email = recipientEmail });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AcceptFriendRequest_AppearsInBothUsersFriendsList()
    {
        var (senderClient, _, senderId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (recipientClient, _, _, recipientEmail) = await TestApi.RegisterAsync(factory.CreateClient());

        await senderClient.PostAsJsonAsync("/api/friends", new { email = recipientEmail });
        var response = await recipientClient.PatchAsJsonAsync($"/api/friends/{senderId}", new { accept = true });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var senderFriends = await senderClient.GetFromJsonAsync<FriendsResult>("/api/friends");
        senderFriends!.Friends.Should().HaveCount(1);
        senderFriends.PendingSent.Should().BeEmpty();

        var recipientFriends = await recipientClient.GetFromJsonAsync<FriendsResult>("/api/friends");
        recipientFriends!.Friends.Should().HaveCount(1);
        recipientFriends.PendingReceived.Should().BeEmpty();
    }

    [Fact]
    public async Task DeclineFriendRequest_RemovedFromPendingNotBlocked()
    {
        var (senderClient, _, senderId, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (recipientClient, _, _, recipientEmail) = await TestApi.RegisterAsync(factory.CreateClient());

        await senderClient.PostAsJsonAsync("/api/friends", new { email = recipientEmail });
        var response = await recipientClient.PatchAsJsonAsync($"/api/friends/{senderId}", new { accept = false });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var recipientFriends = await recipientClient.GetFromJsonAsync<FriendsResult>("/api/friends");
        recipientFriends!.PendingReceived.Should().BeEmpty();
        recipientFriends.Friends.Should().BeEmpty();

        // Sender can re-send after decline
        var resend = await senderClient.PostAsJsonAsync("/api/friends", new { email = recipientEmail });
        resend.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveFriend_Returns204AndRemovedFromBothLists()
    {
        var (client1, _, id1, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (client2, _, id2, email2) = await TestApi.RegisterAsync(factory.CreateClient());

        await client1.PostAsJsonAsync("/api/friends", new { email = email2 });
        await client2.PatchAsJsonAsync($"/api/friends/{id1}", new { accept = true });

        var response = await client1.DeleteAsync($"/api/friends/{id2}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var friends1 = await client1.GetFromJsonAsync<FriendsResult>("/api/friends");
        friends1!.Friends.Should().BeEmpty();

        var friends2 = await client2.GetFromJsonAsync<FriendsResult>("/api/friends");
        friends2!.Friends.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelSentRequest_Returns204AndRemovedFromPendingSent()
    {
        var (senderClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, recipientId, recipientEmail) = await TestApi.RegisterAsync(factory.CreateClient());

        await senderClient.PostAsJsonAsync("/api/friends", new { email = recipientEmail });
        var response = await senderClient.DeleteAsync($"/api/friends/{recipientId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var friends = await senderClient.GetFromJsonAsync<FriendsResult>("/api/friends");
        friends!.PendingSent.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFriends_Unauthenticated_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/friends");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record FriendsResult(
        List<FriendInfo> Friends,
        List<ReceivedInfo> PendingReceived,
        List<SentInfo> PendingSent);

    private record FriendInfo(Guid ProfileId, string DisplayName, string Email);
    private record ReceivedInfo(Guid RequesterId, string DisplayName, string Email);
    private record SentInfo(Guid AddresseeId, string DisplayName, string Email);
    private record SendRequestResult(bool InvitationSent);
}
