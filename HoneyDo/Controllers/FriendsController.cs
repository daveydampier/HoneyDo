using HoneyDo.Common.Extensions;
using HoneyDo.Features.Friends;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyDo.Controllers;

[ApiController]
[Route("api/friends")]
[Authorize]
public class FriendsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<FriendsResult>> Get(CancellationToken ct) =>
        Ok(await mediator.Send(new GetFriendsQuery(User.GetProfileId()), ct));

    [HttpPost]
    public async Task<ActionResult<SendRequestResult>> SendRequest(SendFriendRequestRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new SendFriendRequestCommand(User.GetProfileId(), request.Email), ct);
        return Ok(result);
    }

    [HttpPatch("{requesterId:guid}")]
    public async Task<IActionResult> Respond(Guid requesterId, RespondToRequestRequest request, CancellationToken ct)
    {
        await mediator.Send(new RespondToRequestCommand(User.GetProfileId(), requesterId, request.Accept), ct);
        return NoContent();
    }

    [HttpDelete("{friendId:guid}")]
    public async Task<IActionResult> Remove(Guid friendId, CancellationToken ct)
    {
        await mediator.Send(new RemoveFriendCommand(User.GetProfileId(), friendId), ct);
        return NoContent();
    }
}

public record SendFriendRequestRequest(string Email);
public record RespondToRequestRequest(bool Accept);
