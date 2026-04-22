using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Members;

public record UpdateMemberRoleCommand(Guid ListId, Guid TargetProfileId, Guid ActorId, MemberRole NewRole) : IRequest<MemberResponse>;

public class UpdateMemberRoleCommandValidator : AbstractValidator<UpdateMemberRoleCommand>
{
    public UpdateMemberRoleCommandValidator()
    {
        RuleFor(x => x.NewRole).IsInEnum();
    }
}

public class UpdateMemberRoleCommandHandler(AppDbContext db) : IRequestHandler<UpdateMemberRoleCommand, MemberResponse>
{
    public async Task<MemberResponse> Handle(UpdateMemberRoleCommand request, CancellationToken ct)
    {
        var actorMembership = await db.ListMembers
            .FirstOrDefaultAsync(m => m.ListId == request.ListId && m.ProfileId == request.ActorId, ct)
            ?? throw new NotFoundException();

        if (actorMembership.Role != MemberRole.Owner)
            throw new ForbiddenException("Only the list owner can change member roles.");

        var target = await db.ListMembers
            .Include(m => m.Profile)
            .FirstOrDefaultAsync(m => m.ListId == request.ListId && m.ProfileId == request.TargetProfileId, ct)
            ?? throw new NotFoundException();

        if (target.Role == MemberRole.Owner)
            throw new ForbiddenException("The list owner's role cannot be changed.");

        target.Role = request.NewRole;
        await db.SaveChangesAsync(ct);

        return new MemberResponse(target.ProfileId, target.Profile.DisplayName, target.Profile.AvatarUrl, target.Role, target.JoinedAt);
    }
}
