using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Items;

public record GetItemQuery(Guid ListId, Guid ItemId, Guid ProfileId) : IRequest<TodoItemResponse>;

public class GetItemQueryHandler(AppDbContext db) : IRequestHandler<GetItemQuery, TodoItemResponse>
{
    public async Task<TodoItemResponse> Handle(GetItemQuery request, CancellationToken ct)
    {
        var isMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct);

        if (!isMember) throw new NotFoundException();

        return await db.TodoItems
            .Where(i => i.Id == request.ItemId && i.ListId == request.ListId)
            .Select(TodoItemResponse.Projection)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException();
    }
}
