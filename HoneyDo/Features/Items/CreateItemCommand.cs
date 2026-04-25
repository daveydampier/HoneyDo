using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Common.Services;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Items;

public record CreateItemCommand(
    Guid ListId,
    Guid ProfileId,
    string Content,
    string? Notes,
    string? DueDate,
    Guid? AssignedToId) : IRequest<TodoItemResponse>;

public class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Notes).MaximumLength(256).When(x => x.Notes is not null);
        RuleFor(x => x.DueDate)
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("DueDate must be in YYYY-MM-DD format.")
            .When(x => x.DueDate is not null);
    }
}

public class CreateItemCommandHandler(AppDbContext db, IActivityLogger activityLogger, ILogger<CreateItemCommandHandler> logger)
    : IRequestHandler<CreateItemCommand, TodoItemResponse>
{
    public async Task<TodoItemResponse> Handle(CreateItemCommand request, CancellationToken ct)
    {
        var isMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct);

        if (!isMember) throw new NotFoundException();

        if (request.AssignedToId.HasValue)
        {
            var assigneeIsMember = await db.ListMembers
                .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.AssignedToId, ct);

            if (!assigneeIsMember)
                throw new ValidationException([new FluentValidation.Results.ValidationFailure("AssignedToId", "Assignee must be a member of this list.")]);
        }

        var now = DateTime.UtcNow;
        var item = new TodoItem
        {
            Id           = Guid.NewGuid(),
            ListId       = request.ListId,
            Content      = request.Content,
            StatusId     = (int)ItemStatus.NotStarted,
            Notes        = request.Notes,
            DueDate      = request.DueDate,
            AssignedToId = request.AssignedToId,
            CreatedAt    = now,
            UpdatedAt    = now
        };

        db.TodoItems.Add(item);
        activityLogger.Log(request.ListId, request.ProfileId, "ItemCreated",
            ActivityLogger.Truncate(request.Content), now);

        await db.SaveChangesAsync(ct);

        logger.LogDebug("Item {ItemId} created in list {ListId} by profile {ProfileId}",
            item.Id, request.ListId, request.ProfileId);

        return await db.TodoItems
            .Where(i => i.Id == item.Id)
            .Select(TodoItemResponse.Projection)
            .FirstAsync(ct);
    }
}
