using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Lists;

public record CloseListCommand(Guid ListId, Guid ProfileId) : IRequest<TodoListResponse>;

public class CloseListCommandHandler(AppDbContext db) : IRequestHandler<CloseListCommand, TodoListResponse>
{
    public async Task<TodoListResponse> Handle(CloseListCommand request, CancellationToken ct)
    {
        var membership = await db.ListMembers
            .Include(m => m.List)
            .FirstOrDefaultAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct)
            ?? throw new NotFoundException();

        if (membership.Role != MemberRole.Owner)
            throw new ForbiddenException("Only the list owner can close a list.");

        if (membership.List.ClosedAt is not null)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("ListId", "This list is already closed.")]);

        var items = await db.TodoItems
            .Where(i => i.ListId == request.ListId)
            .Select(i => i.StatusId)
            .ToListAsync(ct);

        if (items.Count == 0)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("ListId", "Cannot close a list with no tasks.")]);

        // Status 1 = Not Started; all other statuses (Partial, Complete, Abandoned) allow closing.
        var hasOpenItems = items.Any(s => s == 1);
        if (hasOpenItems)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("ListId", "All tasks must be Partial, Complete, or Abandoned before closing.")]);

        var now = DateTime.UtcNow;
        membership.List.ClosedAt = now;
        membership.List.UpdatedAt = now;

        db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            ListId = request.ListId,
            ActorId = request.ProfileId,
            ActionType = "ListClosed",
            Timestamp = now
        });

        await db.SaveChangesAsync(ct);

        var memberCount = await db.ListMembers.CountAsync(m => m.ListId == request.ListId, ct);
        var ownerName = await db.ListMembers
            .Where(m => m.ListId == request.ListId && m.Role == MemberRole.Owner)
            .Select(m => m.Profile.DisplayName)
            .FirstOrDefaultAsync(ct) ?? "Unknown";

        return new TodoListResponse(
            membership.List.Id,
            membership.List.Title,
            membership.Role,
            ownerName,
            memberCount,
            items.Count(s => s == 1),
            items.Count(s => s == 2),
            items.Count(s => s == 3),
            items.Count(s => s == 4),
            membership.List.CreatedAt,
            membership.List.UpdatedAt,
            membership.List.ClosedAt,
            []);
    }
}
