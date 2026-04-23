using HoneyDo.Common.Extensions;
using HoneyDo.Features.Lists;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyDo.Controllers;

[ApiController]
[Route("api/lists")]
[Authorize]
public class ListsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoListResponse>>> GetAll(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetListsQuery(User.GetProfileId()), ct));
    }

    [HttpGet("{listId:guid}")]
    public async Task<ActionResult<TodoListResponse>> GetById(Guid listId, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetListQuery(listId, User.GetProfileId()), ct));
    }

    [HttpPost]
    public async Task<ActionResult<TodoListResponse>> Create(CreateListRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateListCommand(request.Title, User.GetProfileId()), ct);
        return CreatedAtAction(nameof(GetById), new { listId = result.Id }, result);
    }

    [HttpPatch("{listId:guid}")]
    public async Task<ActionResult<TodoListResponse>> Update(Guid listId, UpdateListRequest request, CancellationToken ct)
    {
        return Ok(await mediator.Send(new UpdateListCommand(listId, request.Title, User.GetProfileId()), ct));
    }

    [HttpDelete("{listId:guid}")]
    public async Task<IActionResult> Delete(Guid listId, CancellationToken ct)
    {
        await mediator.Send(new DeleteListCommand(listId, User.GetProfileId()), ct);
        return NoContent();
    }

    [HttpPost("{listId:guid}/close")]
    public async Task<ActionResult<TodoListResponse>> Close(Guid listId, CancellationToken ct) =>
        Ok(await mediator.Send(new CloseListCommand(listId, User.GetProfileId()), ct));

    [HttpGet("{listId:guid}/addable-friends")]
    public async Task<ActionResult<IEnumerable<AddableFriendResponse>>> GetAddableFriends(Guid listId, CancellationToken ct) =>
        Ok(await mediator.Send(new GetAddableFriendsQuery(listId, User.GetProfileId()), ct));

    [HttpGet("{listId:guid}/activity")]
    public async Task<ActionResult<IEnumerable<ActivityLogResponse>>> GetActivity(Guid listId, CancellationToken ct) =>
        Ok(await mediator.Send(new GetActivityLogsQuery(listId, User.GetProfileId()), ct));
}

public record CreateListRequest(string Title);
public record UpdateListRequest(string Title);
