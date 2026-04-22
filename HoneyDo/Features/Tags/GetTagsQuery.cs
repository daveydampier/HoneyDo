using HoneyDo.Data;
using HoneyDo.Features.Items;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Tags;

public record GetTagsQuery(Guid ProfileId) : IRequest<IEnumerable<TagDto>>;

public class GetTagsQueryHandler(AppDbContext db) : IRequestHandler<GetTagsQuery, IEnumerable<TagDto>>
{
    public async Task<IEnumerable<TagDto>> Handle(GetTagsQuery request, CancellationToken ct)
    {
        return await db.Tags
            .Where(t => t.ProfileId == request.ProfileId)
            .Select(t => new TagDto(t.Id, t.Name, t.Color))
            .ToListAsync(ct);
    }
}
