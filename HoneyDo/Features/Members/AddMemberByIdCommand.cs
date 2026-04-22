using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Members;

public record AddMemberByIdCommand(Guid ListId, Guid ActorId, Guid ProfileId) : IRequest<MemberResponse>;

public class AddMemberByIdCommandHandler(AppDbContext db) : IRequestHandler<AddMemberByIdCommand, MemberResponse>
{
    public async Task<MemberResponse> Handle(AddMemberByIdCommand request, CancellationToken ct)
    {
        var actorMembership = await db.ListMembers
            .FirstOrDefaultAsync(m => m.ListId == request.ListId && m.ProfileId == request.ActorId, ct)
            ?? throw new NotFoundException();

        if (actorMembership.Role != MemberRole.Owner)
            throw new ForbiddenException("Only the list owner can add members.");

        var invitee = await db.Profiles
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId, ct)
            ?? throw new NotFoundException("Profile not found.");

        var alreadyMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == invitee.Id, ct);

        if (alreadyMember)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("ProfileId", "This user is already a member of the list.")]);

        var now = DateTime.UtcNow;
        var member = new ListMember
        {
            ListId = request.ListId,
            ProfileId = invitee.Id,
            Role = MemberRole.Contributor,
            JoinedAt = now
        };
        db.ListMembers.Add(member);

        db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            ListId = request.ListId,
            ActorId = request.ActorId,
            ActionType = "MemberAdded",
            Timestamp = now
        });

        await db.SaveChangesAsync(ct);

        return new MemberResponse(invitee.Id, invitee.DisplayName, invitee.AvatarUrl, MemberRole.Contributor, now);
    }
}
