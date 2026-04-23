using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Features.Items;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Lists;

public record GetListTagsQuery(Guid ListId, Guid ProfileId) : IRequest<IEnumerable<TagDto>>;

public class GetListTagsQueryHandler(AppDbContext db) : IRequestHandler<GetListTagsQuery, IEnumerable<TagDto>>
{
    public async Task<IEnumerable<TagDto>> Handle(GetListTagsQuery request, CancellationToken ct)
    {
        var isMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct);

        if (!isMember) throw new NotFoundException();

        // Return all tags owned by any current member of this list, ordered by name.
        var memberIds = await db.ListMembers
            .Where(m => m.ListId == request.ListId)
            .Select(m => m.ProfileId)
            .ToListAsync(ct);

        return await db.Tags
            .Where(t => memberIds.Contains(t.ProfileId))
            .OrderBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.Color))
            .ToListAsync(ct);
    }
}
