using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Items;

public record DeleteItemCommand(Guid ListId, Guid ItemId, Guid ProfileId) : IRequest;

public class DeleteItemCommandHandler(AppDbContext db) : IRequestHandler<DeleteItemCommand>
{
    public async Task Handle(DeleteItemCommand request, CancellationToken ct)
    {
        var isMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct);

        if (!isMember) throw new NotFoundException();

        var item = await db.TodoItems
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.ListId == request.ListId, ct)
            ?? throw new NotFoundException();

        item.DeletedAt = DateTime.UtcNow;

        db.ActivityLogs.Add(new Domain.ActivityLog
        {
            Id = Guid.NewGuid(),
            ListId = request.ListId,
            ActorId = request.ProfileId,
            ActionType = "ItemDeleted",
            Timestamp = item.DeletedAt.Value
        });

        await db.SaveChangesAsync(ct);
    }
}
