using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Account;

public record GetProfileQuery(Guid ProfileId) : IRequest<ProfileResponse>;

public record ProfileResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? PhoneNumber,
    string? AvatarUrl,
    DateTime CreatedAt);

public class GetProfileQueryHandler(AppDbContext db) : IRequestHandler<GetProfileQuery, ProfileResponse>
{
    public async Task<ProfileResponse> Handle(GetProfileQuery request, CancellationToken ct)
    {
        return await db.Profiles
            .Where(p => p.Id == request.ProfileId)
            .Select(p => new ProfileResponse(p.Id, p.Email, p.DisplayName, p.PhoneNumber, p.AvatarUrl, p.CreatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException();
    }
}
