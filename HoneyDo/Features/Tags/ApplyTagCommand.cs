using HoneyDo.Common.Exceptions;
using HoneyDo.Common.Services;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Tags;

public record ApplyTagCommand(Guid ListId, Guid ItemId, Guid TagId, Guid ProfileId) : IRequest;

public class ApplyTagCommandHandler(AppDbContext db, IActivityLogger activityLogger)
    : IRequestHandler<ApplyTagCommand>
{
    public async Task Handle(ApplyTagCommand request, CancellationToken ct)
    {
        var isMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct);

        if (!isMember) throw new NotFoundException();

        var itemContent = await db.TodoItems
            .Where(i => i.Id == request.ItemId && i.ListId == request.ListId)
            .Select(i => i.Content)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException();

        // The tag must belong to any current member of this list (not just the caller).
        var tagName = await db.Tags
            .Where(t => t.Id == request.TagId &&
                db.ListMembers.Any(m => m.ListId == request.ListId && m.ProfileId == t.ProfileId))
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException();

        var alreadyApplied = await db.TodoItemTags
            .AnyAsync(t => t.ItemId == request.ItemId && t.TagId == request.TagId, ct);

        if (!alreadyApplied)
        {
            db.TodoItemTags.Add(new TodoItemTag { ItemId = request.ItemId, TagId = request.TagId });
            activityLogger.Log(request.ListId, request.ProfileId, "TagAdded",
                $"\"{tagName}\" on {ActivityLogger.Truncate(itemContent)}");
            await db.SaveChangesAsync(ct);
        }
    }
}
