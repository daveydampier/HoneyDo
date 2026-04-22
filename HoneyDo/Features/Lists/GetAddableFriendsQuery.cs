using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Lists;

public record GetAddableFriendsQuery(Guid ListId, Guid ProfileId) : IRequest<IEnumerable<AddableFriendResponse>>;

public record AddableFriendResponse(Guid ProfileId, string DisplayName, string Email, string? AvatarUrl);

public class GetAddableFriendsQueryHandler(AppDbContext db)
    : IRequestHandler<GetAddableFriendsQuery, IEnumerable<AddableFriendResponse>>
{
    public async Task<IEnumerable<AddableFriendResponse>> Handle(
        GetAddableFriendsQuery request, CancellationToken ct)
    {
        var isMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct);

        if (!isMember) throw new NotFoundException();

        // Collect profile IDs already on the list so we can exclude them.
        var existingIds = await db.ListMembers
            .Where(m => m.ListId == request.ListId)
            .Select(m => m.ProfileId)
            .ToListAsync(ct);

        // Friends where the current user sent the request.
        var sentFriends = await db.Friends
            .Where(f => f.RequesterId == request.ProfileId
                     && f.Status == FriendStatus.Accepted
                     && !existingIds.Contains(f.AddresseeId))
            .Select(f => new AddableFriendResponse(
                f.AddresseeId, f.Addressee.DisplayName, f.Addressee.Email, f.Addressee.AvatarUrl))
            .ToListAsync(ct);

        // Friends where the current user received the request.
        var receivedFriends = await db.Friends
            .Where(f => f.AddresseeId == request.ProfileId
                     && f.Status == FriendStatus.Accepted
                     && !existingIds.Contains(f.RequesterId))
            .Select(f => new AddableFriendResponse(
                f.RequesterId, f.Requester.DisplayName, f.Requester.Email, f.Requester.AvatarUrl))
            .ToListAsync(ct);

        return sentFriends.Concat(receivedFriends).OrderBy(f => f.DisplayName);
    }
}
