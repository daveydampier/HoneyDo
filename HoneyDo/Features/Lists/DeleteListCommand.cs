using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Lists;

public record DeleteListCommand(Guid ListId, Guid ProfileId) : IRequest;

public class DeleteListCommandHandler(AppDbContext db) : IRequestHandler<DeleteListCommand>
{
    public async Task Handle(DeleteListCommand request, CancellationToken ct)
    {
        var membership = await db.ListMembers
            .Include(m => m.List)
            .FirstOrDefaultAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct)
            ?? throw new NotFoundException();

        if (membership.Role != MemberRole.Owner)
            throw new ForbiddenException("Only the list owner can delete a list.");

        membership.List.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
