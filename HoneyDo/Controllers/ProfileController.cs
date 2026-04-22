using HoneyDo.Common.Extensions;
using HoneyDo.Features.Account;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyDo.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetProfileQuery(User.GetProfileId()), ct));
    }

    [HttpPatch]
    public async Task<ActionResult<ProfileResponse>> Update(UpdateProfileRequest request, CancellationToken ct)
    {
        var command = new UpdateProfileCommand(User.GetProfileId(), request.DisplayName, request.PhoneNumber, request.AvatarUrl);
        return Ok(await mediator.Send(command, ct));
    }

    [HttpPatch("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await mediator.Send(new ChangePasswordCommand(User.GetProfileId(), request.CurrentPassword, request.NewPassword), ct);
        return NoContent();
    }
}

public record UpdateProfileRequest(string DisplayName, string? PhoneNumber, string? AvatarUrl);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
