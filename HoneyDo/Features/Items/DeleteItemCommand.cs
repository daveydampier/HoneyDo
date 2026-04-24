using HoneyDo.Common.Exceptions;
using HoneyDo.Common.Services;
using HoneyDo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Items;

public record DeleteItemCommand(Guid ListId, Guid ItemId, Guid ProfileId) : IRequest;

public class DeleteItemCommandHandler(AppDbContext db, IActivityLogger activityLogger)
    : IRequestHandler<DeleteItemCommand>
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

        activityLogger.Log(request.ListId, request.ProfileId, "ItemDeleted",
            ActivityLogger.Truncate(item.Content), item.DeletedAt.Value);

        await db.SaveChangesAsync(ct);
    }
}
