using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Friends;

public record RespondToRequestCommand(Guid ProfileId, Guid RequesterId, bool Accept) : IRequest;

public class RespondToRequestCommandHandler(AppDbContext db) : IRequestHandler<RespondToRequestCommand>
{
    public async Task Handle(RespondToRequestCommand request, CancellationToken ct)
    {
        var friendship = await db.Friends.FirstOrDefaultAsync(
            f => f.RequesterId == request.RequesterId && f.AddresseeId == request.ProfileId && f.Status == FriendStatus.Pending, ct)
            ?? throw new NotFoundException();

        if (request.Accept)
            friendship.Status = FriendStatus.Accepted;
        else
            db.Friends.Remove(friendship);

        await db.SaveChangesAsync(ct);
    }
}
