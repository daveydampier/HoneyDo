using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Lists;

public class ActivityLogTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task GetActivity_NoActions_ReturnsEmptyList()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client, "Fresh List");

        var response = await client.GetAsync($"/api/lists/{listId}/activity");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await response.Content.ReadFromJsonAsync<List<ActivityLogEntry>>();
        logs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActivity_AfterCreateItem_ContainsItemCreatedWithTaskNameDetail()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        await TestApi.CreateItemAsync(client, listId, "Buy oranges");

        var logs = await client.GetFromJsonAsync<List<ActivityLogEntry>>($"/api/lists/{listId}/activity");

        var entry = logs.Should().ContainSingle(l => l.ActionType == "ItemCreated").Subject;
        entry.Detail.Should().Be("Buy oranges");
        entry.ActorName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetActivity_AfterStatusChange_ContainsStatusChangedWithArrowDetail()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId, "Clean the kitchen");

        // Change status to Complete (StatusId 3)
        var res = await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{itemId}", new { statusId = 3 });
        res.EnsureSuccessStatusCode();

        var logs = await client.GetFromJsonAsync<List<ActivityLogEntry>>($"/api/lists/{listId}/activity");

        var entry = logs.Should().ContainSingle(l => l.ActionType == "StatusChanged").Subject;
        entry.Detail.Should().Be("Clean the kitchen → Complete");
    }

    [Fact]
    public async Task GetActivity_AfterStatusChangeToPartial_DetailShowsPartial()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId, "Organise garage");

        // Change status to Partial (StatusId 2)
        var res = await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{itemId}", new { statusId = 2 });
        res.EnsureSuccessStatusCode();

        var logs = await client.GetFromJsonAsync<List<ActivityLogEntry>>($"/api/lists/{listId}/activity");

        var entry = logs.Should().ContainSingle(l => l.ActionType == "StatusChanged").Subject;
        entry.Detail.Should().Be("Organise garage → Partial");
    }

    [Fact]
    public async Task GetActivity_AfterDeleteItem_ContainsItemDeletedWithTaskNameDetail()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId, "Old task to remove");

        var del = await client.DeleteAsync($"/api/lists/{listId}/items/{itemId}");
        del.EnsureSuccessStatusCode();

        var logs = await client.GetFromJsonAsync<List<ActivityLogEntry>>($"/api/lists/{listId}/activity");

        var entry = logs.Should().ContainSingle(l => l.ActionType == "ItemDeleted").Subject;
        entry.Detail.Should().Be("Old task to remove");
    }

    [Fact]
    public async Task GetActivity_AfterAddMember_ContainsMemberAddedWithMemberNameDetail()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (_, _, _, memberEmail) = await TestApi.RegisterAsync(factory.CreateClient(), displayName: "New Member");
        var listId = await TestApi.CreateListAsync(ownerClient);

        await TestApi.AddMemberAsync(ownerClient, listId, memberEmail);

        var logs = await ownerClient.GetFromJsonAsync<List<ActivityLogEntry>>($"/api/lists/{listId}/activity");

        var entry = logs.Should().ContainSingle(l => l.ActionType == "MemberAdded").Subject;
        entry.Detail.Should().Be("New Member");
    }

    [Fact]
    public async Task GetActivity_MultipleActions_ReturnedNewestFirst()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var item1 = await TestApi.CreateItemAsync(client, listId, "First task");
        await TestApi.CreateItemAsync(client, listId, "Second task");

        // Add a status change too
        await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{item1}", new { statusId = 3 });

        var logs = await client.GetFromJsonAsync<List<ActivityLogEntry>>($"/api/lists/{listId}/activity");

        logs.Should().HaveCount(3);
        // Newest first — the status change was last
        logs![0].ActionType.Should().Be("StatusChanged");
    }

    [Fact]
    public async Task GetActivity_NonMember_Returns404()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (otherClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);

        var response = await otherClient.GetAsync($"/api/lists/{listId}/activity");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActivity_Unauthenticated_Returns401()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);

        var response = await factory.CreateClient().GetAsync($"/api/lists/{listId}/activity");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetActivity_ContentExceeds100Chars_DetailIsTruncated()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var longContent = new string('A', 150);
        await TestApi.CreateItemAsync(client, listId, longContent);

        var logs = await client.GetFromJsonAsync<List<ActivityLogEntry>>($"/api/lists/{listId}/activity");

        var entry = logs.Should().ContainSingle(l => l.ActionType == "ItemCreated").Subject;
        entry.Detail.Should().NotBeNull();
        entry.Detail!.Length.Should().BeLessThanOrEqualTo(100);
        entry.Detail.Should().EndWith("…");
    }

    // ── TagAdded / TagRemoved ────────────────────────────────────────────────

    [Fact]
    public async Task GetActivity_AfterTagApplied_ContainsTagAddedEntry()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId, "Tagged task");
        var tagId = await TestApi.CreateTagAsync(client, "Urgent", "#FF0000");

        var res = await client.PostAsJsonAsync($"/api/lists/{listId}/items/{itemId}/tags/{tagId}", new { });
        res.EnsureSuccessStatusCode();

        var logs = await client.GetFromJsonAsync<List<ActivityLogEntry>>($"/api/lists/{listId}/activity");

        var entry = logs.Should().ContainSingle(l => l.ActionType == "TagAdded").Subject;
        entry.Detail.Should().Contain("Urgent");
        entry.Detail.Should().Contain("Tagged task");
        entry.ActorName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetActivity_AfterTagRemoved_ContainsTagRemovedEntry()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId, "Tagged task");
        var tagId = await TestApi.CreateTagAsync(client, "Urgent", "#FF0000");

        await client.PostAsJsonAsync($"/api/lists/{listId}/items/{itemId}/tags/{tagId}", new { });
        var del = await client.DeleteAsync($"/api/lists/{listId}/items/{itemId}/tags/{tagId}");
        del.EnsureSuccessStatusCode();

        var logs = await client.GetFromJsonAsync<List<ActivityLogEntry>>($"/api/lists/{listId}/activity");

        var entry = logs.Should().ContainSingle(l => l.ActionType == "TagRemoved").Subject;
        entry.Detail.Should().Contain("Urgent");
        entry.Detail.Should().Contain("Tagged task");
    }

    [Fact]
    public async Task GetActivity_ApplyTagTwice_LogsOnlyOneTagAddedEntry()
    {
        // Applying an already-applied tag is idempotent — should not produce a second log entry
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId, "Task");
        var tagId = await TestApi.CreateTagAsync(client, "Work", "#0000FF");

        await client.PostAsJsonAsync($"/api/lists/{listId}/items/{itemId}/tags/{tagId}", new { });
        await client.PostAsJsonAsync($"/api/lists/{listId}/items/{itemId}/tags/{tagId}", new { });

        var logs = await client.GetFromJsonAsync<List<ActivityLogEntry>>($"/api/lists/{listId}/activity");

        logs!.Count(l => l.ActionType == "TagAdded").Should().Be(1);
    }

    // ── NotesUpdated ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActivity_AfterNotesUpdated_ContainsNotesUpdatedEntry()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId, "Important task");

        var res = await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{itemId}", new { notes = "Remember to check the manual." });
        res.EnsureSuccessStatusCode();

        var logs = await client.GetFromJsonAsync<List<ActivityLogEntry>>($"/api/lists/{listId}/activity");

        var entry = logs.Should().ContainSingle(l => l.ActionType == "NotesUpdated").Subject;
        entry.Detail.Should().Be("Important task");
        entry.ActorName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetActivity_SameNotesSentTwice_LogsNotesUpdatedOnlyOnce()
    {
        // Patching with the same notes value twice should not produce a second log entry
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId, "Task");

        await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{itemId}", new { notes = "Same note." });
        await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{itemId}", new { notes = "Same note." });

        var logs = await client.GetFromJsonAsync<List<ActivityLogEntry>>($"/api/lists/{listId}/activity");

        logs!.Count(l => l.ActionType == "NotesUpdated").Should().Be(1);
    }

    [Fact]
    public async Task GetActivity_PatchWithoutNotesField_DoesNotLogNotesUpdated()
    {
        // Omitting the notes field entirely in a PATCH should not trigger a NotesUpdated entry
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId, "Task");

        // Patch only the status — notes field absent
        await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{itemId}", new { statusId = 2 });

        var logs = await client.GetFromJsonAsync<List<ActivityLogEntry>>($"/api/lists/{listId}/activity");

        logs!.Should().NotContain(l => l.ActionType == "NotesUpdated");
    }

    private record ActivityLogEntry(Guid Id, string ActionType, string ActorName, string? Detail, DateTime Timestamp);
}
