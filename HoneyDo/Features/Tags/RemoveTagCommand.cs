using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Tags;

public record RemoveTagCommand(Guid ListId, Guid ItemId, Guid TagId, Guid ProfileId) : IRequest;

public class RemoveTagCommandHandler(AppDbContext db) : IRequestHandler<RemoveTagCommand>
{
    public async Task Handle(RemoveTagCommand request, CancellationToken ct)
    {
        var isMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct);

        if (!isMember) throw new NotFoundException();

        var itemTag = await db.TodoItemTags
            .FirstOrDefaultAsync(t => t.ItemId == request.ItemId && t.TagId == request.TagId, ct)
            ?? throw new NotFoundException();

        db.TodoItemTags.Remove(itemTag);
        await db.SaveChangesAsync(ct);
    }
}
