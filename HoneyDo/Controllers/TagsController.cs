using HoneyDo.Common.Extensions;
using HoneyDo.Features.Items;
using HoneyDo.Features.Tags;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyDo.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
public class TagsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TagDto>>> GetAll(CancellationToken ct) =>
        Ok(await mediator.Send(new GetTagsQuery(User.GetProfileId()), ct));

    [HttpPost]
    public async Task<ActionResult<TagDto>> Create(CreateTagRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new CreateTagCommand(User.GetProfileId(), request.Name, request.Color), ct));

    [HttpDelete("{tagId:guid}")]
    public async Task<IActionResult> Delete(Guid tagId, CancellationToken ct)
    {
        await mediator.Send(new DeleteTagCommand(tagId, User.GetProfileId()), ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/lists/{listId:guid}/items/{itemId:guid}/tags")]
[Authorize]
public class ItemTagsController(IMediator mediator) : ControllerBase
{
    [HttpPost("{tagId:guid}")]
    public async Task<IActionResult> Apply(Guid listId, Guid itemId, Guid tagId, CancellationToken ct)
    {
        await mediator.Send(new ApplyTagCommand(listId, itemId, tagId, User.GetProfileId()), ct);
        return NoContent();
    }

    [HttpDelete("{tagId:guid}")]
    public async Task<IActionResult> Remove(Guid listId, Guid itemId, Guid tagId, CancellationToken ct)
    {
        await mediator.Send(new RemoveTagCommand(listId, itemId, tagId, User.GetProfileId()), ct);
        return NoContent();
    }
}

public record CreateTagRequest(string Name, string Color);
