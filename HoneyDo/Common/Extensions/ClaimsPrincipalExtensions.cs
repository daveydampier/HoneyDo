using HoneyDo.Common.Exceptions;
using System.Security.Claims;

namespace HoneyDo.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetProfileId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedException();

        return Guid.TryParse(claim, out var id)
            ? id
            : throw new UnauthorizedException();
    }
}
