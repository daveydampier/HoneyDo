using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Invitations;

public record AcceptInvitationCommand(Guid AcceptorId, string Token) : IRequest;

public class AcceptInvitationCommandHandler(AppDbContext db) : IRequestHandler<AcceptInvitationCommand>
{
    public async Task Handle(AcceptInvitationCommand request, CancellationToken ct)
    {
        var invitation = await db.Invitations
            .FirstOrDefaultAsync(i => i.Token == request.Token && i.AcceptedAt == null, ct)
            ?? throw new NotFoundException();

        // Edge case: the inviter registered themselves via the link (same account).
        if (invitation.InviterId != request.AcceptorId)
        {
            // Only create the friend request if one doesn't already exist.
            var alreadyConnected = await db.Friends.AnyAsync(
                f => (f.RequesterId == invitation.InviterId && f.AddresseeId == request.AcceptorId) ||
                     (f.RequesterId == request.AcceptorId && f.AddresseeId == invitation.InviterId), ct);

            if (!alreadyConnected)
            {
                db.Friends.Add(new Friend
                {
                    RequesterId = invitation.InviterId,
                    AddresseeId = request.AcceptorId,
                    Status = FriendStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        invitation.AcceptedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
