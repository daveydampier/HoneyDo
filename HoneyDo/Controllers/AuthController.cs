using HoneyDo.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HoneyDo.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Register), result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }
}
