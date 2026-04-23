using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Tags;

public record ApplyTagCommand(Guid ListId, Guid ItemId, Guid TagId, Guid ProfileId) : IRequest;

public class ApplyTagCommandHandler(AppDbContext db) : IRequestHandler<ApplyTagCommand>
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
            db.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                ListId = request.ListId,
                ActorId = request.ProfileId,
                ActionType = "TagAdded",
                Detail = $"\"{tagName}\" on {Truncate(itemContent)}",
                Timestamp = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }
    }

    private static string Truncate(string s) => s.Length > 80 ? s[..77] + "…" : s;
}
