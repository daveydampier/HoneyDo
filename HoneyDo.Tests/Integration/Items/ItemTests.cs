using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Items;

public class ItemTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task GetItems_EmptyList_ReturnsEmptyPagedResult()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);

        var response = await client.GetAsync($"/api/lists/{listId}/items");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse>();
        body!.Items.Should().BeEmpty();
        body.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetItems_Unauthenticated_Returns401()
    {
        var response = await factory.CreateClient().GetAsync($"/api/lists/{Guid.NewGuid()}/items");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetItems_NonMember_Returns404()
    {
        var (ownerClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var (otherClient, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(ownerClient);

        var response = await otherClient.GetAsync($"/api/lists/{listId}/items");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateItem_ValidData_Returns201()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);

        var response = await client.PostAsJsonAsync($"/api/lists/{listId}/items", new
        {
            content = "Buy milk",
            dueDate = "2026-12-31"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ItemResponse>();
        body!.Content.Should().Be("Buy milk");
        body.DueDate.Should().Be("2026-12-31");
        body.Status.Id.Should().Be(1);
    }

    [Fact]
    public async Task CreateItem_EmptyContent_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);

        var response = await client.PostAsJsonAsync($"/api/lists/{listId}/items", new { content = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateItem_InvalidDueDateFormat_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);

        var response = await client.PostAsJsonAsync($"/api/lists/{listId}/items", new
        {
            content = "Valid content",
            dueDate = "31/12/2026"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetItemById_Returns200()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId, "Test task");

        var response = await client.GetAsync($"/api/lists/{listId}/items/{itemId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ItemResponse>();
        body!.Id.Should().Be(itemId);
        body.Content.Should().Be("Test task");
    }

    [Fact]
    public async Task UpdateItem_ChangeStatus_Returns200WithNewStatus()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId);

        var response = await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{itemId}", new { statusId = 3 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ItemResponse>();
        body!.Status.Id.Should().Be(3);
        body.Status.Name.Should().Be("Complete");
    }

    [Fact]
    public async Task UpdateItem_ChangeContent_Returns200()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId, "Original");

        var response = await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{itemId}", new { content = "Updated" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ItemResponse>();
        body!.Content.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateItem_ClearDueDate_Returns200WithNullDueDate()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var res = await client.PostAsJsonAsync($"/api/lists/{listId}/items", new { content = "Task", dueDate = "2026-12-31" });
        var created = await res.Content.ReadFromJsonAsync<ItemResponse>();

        var response = await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{created!.Id}", new { clearDueDate = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ItemResponse>();
        body!.DueDate.Should().BeNull();
    }

    [Fact]
    public async Task DeleteItem_Returns204AndItemNoLongerExists()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId);

        var deleteResponse = await client.DeleteAsync($"/api/lists/{listId}/items/{itemId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/lists/{listId}/items/{itemId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetItems_WithSearch_ReturnsMatchingItems()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        await TestApi.CreateItemAsync(client, listId, "Buy groceries");
        await TestApi.CreateItemAsync(client, listId, "Call dentist");

        var response = await client.GetAsync($"/api/lists/{listId}/items?search=groceries");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse>();
        body!.Items.Should().HaveCount(1);
        body.Items[0].Content.Should().Be("Buy groceries");
    }

    [Fact]
    public async Task GetItems_WithStatusFilter_ReturnsMatchingItems()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);
        var itemId = await TestApi.CreateItemAsync(client, listId, "Complete me");
        await TestApi.CreateItemAsync(client, listId, "Not started");
        await client.PatchAsJsonAsync($"/api/lists/{listId}/items/{itemId}", new { statusId = 3 });

        var response = await client.GetAsync($"/api/lists/{listId}/items?statusIds=3");

        var body = await response.Content.ReadFromJsonAsync<PagedResponse>();
        body!.Items.Should().HaveCount(1);
        body.Items[0].Status.Id.Should().Be(3);
    }

    [Fact]
    public async Task GetItems_PageSizeOver100_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);

        var response = await client.GetAsync($"/api/lists/{listId}/items?pageSize=500");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetItems_Pagination_ReturnsCorrectPage()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());
        var listId = await TestApi.CreateListAsync(client);

        for (var i = 1; i <= 5; i++)
            await TestApi.CreateItemAsync(client, listId, $"Item {i}");

        var response = await client.GetAsync($"/api/lists/{listId}/items?page=1&pageSize=2");

        var body = await response.Content.ReadFromJsonAsync<PagedResponse>();
        body!.Items.Should().HaveCount(2);
        body.TotalCount.Should().Be(5);
        body.TotalPages.Should().Be(3);
        body.HasNextPage.Should().BeTrue();
    }

    private record PagedResponse(List<ItemResponse> Items, int TotalCount, int Page, int PageSize, int TotalPages, bool HasNextPage, bool HasPreviousPage);
    private record ItemResponse(Guid Id, Guid ListId, string Content, StatusInfo Status, string? DueDate, string? Notes);
    private record StatusInfo(int Id, string Name);
}
