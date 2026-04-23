using HoneyDo.Common.Extensions;
using HoneyDo.Common.Models;
using HoneyDo.Features.Items;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyDo.Controllers;

[ApiController]
[Route("api/lists/{listId:guid}/items")]
[Authorize]
public class ItemsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<TodoItemResponse>>> GetAll(
        Guid listId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int[]? statusIds = null,
        [FromQuery] ItemSortBy sortBy = ItemSortBy.CreatedAt,
        [FromQuery] bool ascending = true,
        CancellationToken ct = default)
    {
        var query = new GetItemsQuery(listId, User.GetProfileId(), page, pageSize, search, statusIds, sortBy, ascending);
        return Ok(await mediator.Send(query, ct));
    }

    [HttpGet("{itemId:guid}")]
    public async Task<ActionResult<TodoItemResponse>> GetById(Guid listId, Guid itemId, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetItemQuery(listId, itemId, User.GetProfileId()), ct));
    }

    [HttpPost]
    public async Task<ActionResult<TodoItemResponse>> Create(Guid listId, CreateItemRequest request, CancellationToken ct)
    {
        var command = new CreateItemCommand(listId, User.GetProfileId(), request.Content, request.Notes, request.DueDate, request.AssignedToId);
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { listId, itemId = result.Id }, result);
    }

    [HttpPatch("{itemId:guid}")]
    public async Task<ActionResult<TodoItemResponse>> Update(Guid listId, Guid itemId, UpdateItemRequest request, CancellationToken ct)
    {
        var command = new UpdateItemCommand(listId, itemId, User.GetProfileId(),
            request.Content, request.StatusId, request.Notes, request.DueDate,
            request.AssignedToId, request.ClearDueDate, request.ClearAssignee);
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Delete(Guid listId, Guid itemId, CancellationToken ct)
    {
        await mediator.Send(new DeleteItemCommand(listId, itemId, User.GetProfileId()), ct);
        return NoContent();
    }
}

public record CreateItemRequest(string Content, string? Notes, string? DueDate, Guid? AssignedToId);
public record UpdateItemRequest(string? Content, int? StatusId, string? Notes, string? DueDate, Guid? AssignedToId, bool ClearDueDate = false, bool ClearAssignee = false);
