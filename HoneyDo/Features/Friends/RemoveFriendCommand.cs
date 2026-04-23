using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Friends;

public record RemoveFriendCommand(Guid ProfileId, Guid FriendId) : IRequest;

public class RemoveFriendCommandHandler(AppDbContext db) : IRequestHandler<RemoveFriendCommand>
{
    public async Task Handle(RemoveFriendCommand request, CancellationToken ct)
    {
        var friendship = await db.Friends.FirstOrDefaultAsync(
            f => (f.RequesterId == request.ProfileId && f.AddresseeId == request.FriendId) ||
                 (f.RequesterId == request.FriendId && f.AddresseeId == request.ProfileId), ct)
            ?? throw new NotFoundException();

        db.Friends.Remove(friendship);
        await db.SaveChangesAsync(ct);
    }
}
