using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Members;

public record GetMembersQuery(Guid ListId, Guid ProfileId) : IRequest<IEnumerable<MemberResponse>>;

public record MemberResponse(Guid ProfileId, string DisplayName, string? AvatarUrl, MemberRole Role, DateTime JoinedAt);

public class GetMembersQueryHandler(AppDbContext db) : IRequestHandler<GetMembersQuery, IEnumerable<MemberResponse>>
{
    public async Task<IEnumerable<MemberResponse>> Handle(GetMembersQuery request, CancellationToken ct)
    {
        var isMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct);

        if (!isMember) throw new NotFoundException();

        return await db.ListMembers
            .Where(m => m.ListId == request.ListId)
            .Select(m => new MemberResponse(m.ProfileId, m.Profile.DisplayName, m.Profile.AvatarUrl, m.Role, m.JoinedAt))
            .ToListAsync(ct);
    }
}
