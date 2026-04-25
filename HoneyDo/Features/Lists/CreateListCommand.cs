using FluentValidation;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Lists;

public record CreateListCommand(string Title, Guid ProfileId, string OwnerDisplayName) : IRequest<TodoListResponse>;

public class CreateListCommandValidator : AbstractValidator<CreateListCommand>
{
    public CreateListCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
    }
}

public class CreateListCommandHandler(AppDbContext db) : IRequestHandler<CreateListCommand, TodoListResponse>
{
    public async Task<TodoListResponse> Handle(CreateListCommand request, CancellationToken ct)
    {
        // Enforce title uniqueness per owner at application layer
        var duplicate = await db.ListMembers
            .AnyAsync(m => m.ProfileId == request.ProfileId
                        && m.Role == MemberRole.Owner
                        && m.List.Title == request.Title, ct);

        if (duplicate)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("Title", "You already have a list with this title.")]);

        var now = DateTime.UtcNow;
        var list = new TodoList { Id = Guid.NewGuid(), Title = request.Title, CreatedAt = now, UpdatedAt = now };
        var membership = new ListMember { ListId = list.Id, ProfileId = request.ProfileId, Role = MemberRole.Owner, JoinedAt = now };

        db.TodoLists.Add(list);
        db.ListMembers.Add(membership);
        await db.SaveChangesAsync(ct);

        // New list: only the owner, no contributors, no items, no tags yet.
        // OwnerDisplayName comes from the caller's JWT claim — no extra DB round trip needed.
        return new TodoListResponse(list.Id, list.Title, MemberRole.Owner, request.OwnerDisplayName, [], 1, 0, 0, 0, 0, list.CreatedAt, list.UpdatedAt, null, []);
    }
}
