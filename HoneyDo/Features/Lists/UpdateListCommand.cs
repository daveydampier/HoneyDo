using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Lists;

public record UpdateListCommand(Guid ListId, string Title, Guid ProfileId) : IRequest<TodoListResponse>;

public class UpdateListCommandValidator : AbstractValidator<UpdateListCommand>
{
    public UpdateListCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
    }
}

public class UpdateListCommandHandler(AppDbContext db) : IRequestHandler<UpdateListCommand, TodoListResponse>
{
    public async Task<TodoListResponse> Handle(UpdateListCommand request, CancellationToken ct)
    {
        var membership = await db.ListMembers
            .Include(m => m.List)
            .FirstOrDefaultAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct)
            ?? throw new NotFoundException();

        if (membership.Role != MemberRole.Owner)
            throw new ForbiddenException("Only the list owner can rename a list.");

        membership.List.Title = request.Title;
        membership.List.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var memberCount = await db.ListMembers.CountAsync(m => m.ListId == request.ListId, ct);

        var statusCounts = await db.TodoItems
            .Where(i => i.ListId == request.ListId)
            .GroupBy(i => i.StatusId)
            .Select(g => new { StatusId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

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
            statusCounts.FirstOrDefault(s => s.StatusId == 1)?.Count ?? 0,
            statusCounts.FirstOrDefault(s => s.StatusId == 2)?.Count ?? 0,
            statusCounts.FirstOrDefault(s => s.StatusId == 3)?.Count ?? 0,
            statusCounts.FirstOrDefault(s => s.StatusId == 4)?.Count ?? 0,
            membership.List.CreatedAt,
            membership.List.UpdatedAt,
            membership.List.ClosedAt,
            []);
    }
}
