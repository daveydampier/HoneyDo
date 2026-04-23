using FluentValidation;
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

        var itemExists = await db.TodoItems
            .AnyAsync(i => i.Id == request.ItemId && i.ListId == request.ListId, ct);

        if (!itemExists) throw new NotFoundException();

        var tagOwned = await db.Tags
            .AnyAsync(t => t.Id == request.TagId && t.ProfileId == request.ProfileId, ct);

        if (!tagOwned) throw new NotFoundException();

        var alreadyApplied = await db.TodoItemTags
            .AnyAsync(t => t.ItemId == request.ItemId && t.TagId == request.TagId, ct);

        if (!alreadyApplied)
        {
            db.TodoItemTags.Add(new TodoItemTag { ItemId = request.ItemId, TagId = request.TagId });
            await db.SaveChangesAsync(ct);
        }
    }
}
