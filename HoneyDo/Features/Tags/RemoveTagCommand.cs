using HoneyDo.Common.Exceptions;
using HoneyDo.Common.Services;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Tags;

public record RemoveTagCommand(Guid ListId, Guid ItemId, Guid TagId, Guid ProfileId) : IRequest;

public class RemoveTagCommandHandler(AppDbContext db, IActivityLogger activityLogger)
    : IRequestHandler<RemoveTagCommand>
{
    public async Task Handle(RemoveTagCommand request, CancellationToken ct)
    {
        var isMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct);

        if (!isMember) throw new NotFoundException();

        // Load item content and tag name together for the activity entry.
        var itemContent = await db.TodoItems
            .Where(i => i.Id == request.ItemId && i.ListId == request.ListId)
            .Select(i => i.Content)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException();

        var tagName = await db.Tags
            .Where(t => t.Id == request.TagId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException();

        var itemTag = await db.TodoItemTags
            .FirstOrDefaultAsync(t => t.ItemId == request.ItemId && t.TagId == request.TagId, ct)
            ?? throw new NotFoundException();

        db.TodoItemTags.Remove(itemTag);
        activityLogger.Log(request.ListId, request.ProfileId, "TagRemoved",
            $"\"{tagName}\" from {ActivityLogger.Truncate(itemContent)}");
        await db.SaveChangesAsync(ct);
    }
}
