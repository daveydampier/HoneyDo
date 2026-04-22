using HoneyDo.Common.Extensions;
using HoneyDo.Features.Invitations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyDo.Controllers;

[ApiController]
[Route("api/invitations")]
[Authorize]
public class InvitationsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Called by the frontend immediately after a new user registers via an invite link.
    /// Marks the invitation as accepted and creates a pending friend request from the inviter.
    /// </summary>
    [HttpPost("accept")]
    public async Task<IActionResult> Accept(AcceptInvitationRequest request, CancellationToken ct)
    {
        await mediator.Send(new AcceptInvitationCommand(User.GetProfileId(), request.Token), ct);
        return NoContent();
    }
}

public record AcceptInvitationRequest(string Token);
