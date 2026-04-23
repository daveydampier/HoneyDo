using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Items;

public record UpdateItemCommand(
    Guid ListId,
    Guid ItemId,
    Guid ProfileId,
    string? Content,
    int? StatusId,
    string? Notes,
    string? DueDate,
    Guid? AssignedToId,
    bool ClearDueDate = false,
    bool ClearAssignee = false,
    bool? IsStarred = null) : IRequest<TodoItemResponse>;

public class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(512).When(x => x.Content is not null);
        RuleFor(x => x.StatusId).InclusiveBetween(1, 4).When(x => x.StatusId is not null);
        RuleFor(x => x.Notes).MaximumLength(256).When(x => x.Notes is not null);
        RuleFor(x => x.DueDate)
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("DueDate must be in YYYY-MM-DD format.")
            .When(x => x.DueDate is not null);
    }
}

public class UpdateItemCommandHandler(AppDbContext db) : IRequestHandler<UpdateItemCommand, TodoItemResponse>
{
    public async Task<TodoItemResponse> Handle(UpdateItemCommand request, CancellationToken ct)
    {
        var isMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct);

        if (!isMember) throw new NotFoundException();

        var item = await db.TodoItems
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.ListId == request.ListId, ct)
            ?? throw new NotFoundException();

        if (request.AssignedToId.HasValue)
        {
            var assigneeIsMember = await db.ListMembers
                .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.AssignedToId, ct);

            if (!assigneeIsMember)
                throw new ValidationException([new FluentValidation.Results.ValidationFailure("AssignedToId", "Assignee must be a member of this list.")]);
        }

        var previousStatus = item.StatusId;

        if (request.Content is not null) item.Content = request.Content;
        if (request.StatusId is not null) item.StatusId = request.StatusId.Value;
        if (request.Notes is not null) item.Notes = request.Notes;
        if (request.DueDate is not null) item.DueDate = request.DueDate;
        if (request.ClearDueDate) item.DueDate = null;
        if (request.AssignedToId.HasValue) item.AssignedToId = request.AssignedToId;
        if (request.ClearAssignee) item.AssignedToId = null;
        if (request.IsStarred is not null) item.IsStarred = request.IsStarred.Value;
        item.UpdatedAt = DateTime.UtcNow;

        if (previousStatus != item.StatusId)
        {
            db.ActivityLogs.Add(new Domain.ActivityLog
            {
                Id = Guid.NewGuid(),
                ListId = request.ListId,
                ActorId = request.ProfileId,
                ActionType = "StatusChanged",
                Detail = $"{Truncate(item.Content)} → {StatusName(item.StatusId)}",
                Timestamp = item.UpdatedAt
            });
        }

        await db.SaveChangesAsync(ct);

        return await db.TodoItems
            .Where(i => i.Id == item.Id)
            .Select(TodoItemResponse.Projection)
            .FirstAsync(ct);
    }

    private static string Truncate(string s) =>
        s.Length > 80 ? s[..77] + "…" : s;

    private static string StatusName(int id) => id switch
    {
        1 => "Not Started",
        2 => "Partial",
        3 => "Complete",
        4 => "Abandoned",
        _ => "Unknown"
    };
}
