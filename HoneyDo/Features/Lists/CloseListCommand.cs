using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Common.Services;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Lists;

public record CloseListCommand(Guid ListId, Guid ProfileId) : IRequest<TodoListResponse>;

public class CloseListCommandHandler(AppDbContext db, IActivityLogger activityLogger)
    : IRequestHandler<CloseListCommand, TodoListResponse>
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

        var hasOpenItems = items.Any(s => s == (int)ItemStatus.NotStarted);
        if (hasOpenItems)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("ListId", "All tasks must be Partial, Complete, or Abandoned before closing.")]);

        var now = DateTime.UtcNow;
        membership.List.ClosedAt  = now;
        membership.List.UpdatedAt = now;

        activityLogger.Log(request.ListId, request.ProfileId, "ListClosed", timestamp: now);

        await db.SaveChangesAsync(ct);

        var memberNames = await db.ListMembers
            .Where(m => m.ListId == request.ListId)
            .Select(m => new { m.Role, m.Profile.DisplayName })
            .ToListAsync(ct);

        var ownerName        = memberNames.FirstOrDefault(m => m.Role == MemberRole.Owner)?.DisplayName ?? "Unknown";
        var contributorNames = memberNames.Where(m => m.Role == MemberRole.Contributor).Select(m => m.DisplayName).ToList();

        return new TodoListResponse(
            membership.List.Id,
            membership.List.Title,
            membership.Role,
            ownerName,
            contributorNames,
            memberNames.Count,
            items.Count(s => s == (int)ItemStatus.NotStarted),
            items.Count(s => s == (int)ItemStatus.Partial),
            items.Count(s => s == (int)ItemStatus.Complete),
            items.Count(s => s == (int)ItemStatus.Abandoned),
            membership.List.CreatedAt,
            membership.List.UpdatedAt,
            membership.List.ClosedAt,
            []);
    }
}
