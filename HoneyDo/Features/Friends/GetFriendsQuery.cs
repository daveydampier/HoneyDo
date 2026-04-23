using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Friends;

public record GetFriendsQuery(Guid ProfileId) : IRequest<FriendsResult>;

public record FriendsResult(
    IReadOnlyList<FriendResponse> Friends,
    IReadOnlyList<ReceivedRequestResponse> PendingReceived,
    IReadOnlyList<SentRequestResponse> PendingSent);

public class GetFriendsQueryHandler(AppDbContext db) : IRequestHandler<GetFriendsQuery, FriendsResult>
{
    public async Task<FriendsResult> Handle(GetFriendsQuery request, CancellationToken ct)
    {
        var id = request.ProfileId;

        var accepted = await db.Friends
            .Where(f => (f.RequesterId == id || f.AddresseeId == id) && f.Status == FriendStatus.Accepted)
            .Select(f => f.RequesterId == id
                ? new FriendResponse(f.AddresseeId, f.Addressee.DisplayName, f.Addressee.Email, f.Addressee.AvatarUrl)
                : new FriendResponse(f.RequesterId, f.Requester.DisplayName, f.Requester.Email, f.Requester.AvatarUrl))
            .ToListAsync(ct);

        var pendingReceived = await db.Friends
            .Where(f => f.AddresseeId == id && f.Status == FriendStatus.Pending)
            .Select(f => new ReceivedRequestResponse(f.RequesterId, f.Requester.DisplayName, f.Requester.Email, f.Requester.AvatarUrl, f.CreatedAt))
            .ToListAsync(ct);

        var pendingSent = await db.Friends
            .Where(f => f.RequesterId == id && f.Status == FriendStatus.Pending)
            .Select(f => new SentRequestResponse(f.AddresseeId, f.Addressee.DisplayName, f.Addressee.Email, f.Addressee.AvatarUrl, f.CreatedAt))
            .ToListAsync(ct);

        return new FriendsResult(accepted, pendingReceived, pendingSent);
    }
}
