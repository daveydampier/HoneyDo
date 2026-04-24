using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Common.Services;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Members;

public record AddMemberCommand(Guid ListId, Guid ActorId, string Email, MemberRole Role) : IRequest<MemberResponse>;

public class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class AddMemberCommandHandler(AppDbContext db, IActivityLogger activityLogger, ILogger<AddMemberCommandHandler> logger)
    : IRequestHandler<AddMemberCommand, MemberResponse>
{
    public async Task<MemberResponse> Handle(AddMemberCommand request, CancellationToken ct)
    {
        var actorMembership = await db.ListMembers
            .FirstOrDefaultAsync(m => m.ListId == request.ListId && m.ProfileId == request.ActorId, ct)
            ?? throw new NotFoundException();

        if (actorMembership.Role != MemberRole.Owner)
            throw new ForbiddenException("Only the list owner can add members.");

        var invitee = await db.Profiles
            .FirstOrDefaultAsync(p => p.Email == request.Email.ToLower(), ct)
            ?? throw new NotFoundException("No account found with that email.");

        var alreadyMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == invitee.Id, ct);

        if (alreadyMember)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("Email", "This user is already a member of the list.")]);

        var now = DateTime.UtcNow;
        var member = new ListMember { ListId = request.ListId, ProfileId = invitee.Id, Role = request.Role, JoinedAt = now };
        db.ListMembers.Add(member);

        activityLogger.Log(request.ListId, request.ActorId, "MemberAdded", invitee.DisplayName, now);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Profile {InviteeId} added to list {ListId} as {Role} by {ActorId}",
            invitee.Id, request.ListId, request.Role, request.ActorId);

        return new MemberResponse(invitee.Id, invitee.DisplayName, invitee.AvatarUrl, request.Role, now);
    }
}
