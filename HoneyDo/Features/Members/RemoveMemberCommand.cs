using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Members;

public record RemoveMemberCommand(Guid ListId, Guid TargetProfileId, Guid ActorId) : IRequest;

public class RemoveMemberCommandHandler(AppDbContext db) : IRequestHandler<RemoveMemberCommand>
{
    public async Task Handle(RemoveMemberCommand request, CancellationToken ct)
    {
        var actorMembership = await db.ListMembers
            .FirstOrDefaultAsync(m => m.ListId == request.ListId && m.ProfileId == request.ActorId, ct)
            ?? throw new NotFoundException();

        if (actorMembership.Role != MemberRole.Owner)
            throw new ForbiddenException("Only the list owner can remove members.");

        var target = await db.ListMembers
            .FirstOrDefaultAsync(m => m.ListId == request.ListId && m.ProfileId == request.TargetProfileId, ct)
            ?? throw new NotFoundException();

        if (target.Role == MemberRole.Owner)
            throw new ForbiddenException("The list owner cannot be removed.");

        db.ListMembers.Remove(target);
        await db.SaveChangesAsync(ct);
    }
}
