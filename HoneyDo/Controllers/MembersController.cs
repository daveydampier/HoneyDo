using HoneyDo.Common.Extensions;
using HoneyDo.Domain;
using HoneyDo.Features.Members;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyDo.Controllers;

[ApiController]
[Route("api/lists/{listId:guid}/members")]
[Authorize]
public class MembersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberResponse>>> GetAll(Guid listId, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetMembersQuery(listId, User.GetProfileId()), ct));
    }

    [HttpPost]
    public async Task<ActionResult<MemberResponse>> Add(Guid listId, AddMemberRequest request, CancellationToken ct)
    {
        return Ok(await mediator.Send(new AddMemberCommand(listId, User.GetProfileId(), request.Email, request.Role), ct));
    }

    [HttpPost("{profileId:guid}")]
    public async Task<ActionResult<MemberResponse>> AddById(Guid listId, Guid profileId, CancellationToken ct)
    {
        return Ok(await mediator.Send(new AddMemberByIdCommand(listId, User.GetProfileId(), profileId), ct));
    }

    [HttpPatch("{profileId:guid}")]
    public async Task<ActionResult<MemberResponse>> UpdateRole(Guid listId, Guid profileId, UpdateMemberRoleRequest request, CancellationToken ct)
    {
        return Ok(await mediator.Send(new UpdateMemberRoleCommand(listId, profileId, User.GetProfileId(), request.Role), ct));
    }

    [HttpDelete("{profileId:guid}")]
    public async Task<IActionResult> Remove(Guid listId, Guid profileId, CancellationToken ct)
    {
        await mediator.Send(new RemoveMemberCommand(listId, profileId, User.GetProfileId()), ct);
        return NoContent();
    }
}

public record AddMemberRequest(string Email, MemberRole Role);
public record UpdateMemberRoleRequest(MemberRole Role);
