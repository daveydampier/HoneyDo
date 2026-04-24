using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
using HoneyDo.Features.Items;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Lists;

public record GetListQuery(Guid ListId, Guid ProfileId) : IRequest<TodoListResponse>;

public class GetListQueryHandler(AppDbContext db) : IRequestHandler<GetListQuery, TodoListResponse>
{
    public async Task<TodoListResponse> Handle(GetListQuery request, CancellationToken ct)
    {
        var membership = await db.ListMembers
            .Where(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId)
            .Select(m => new
            {
                m.ListId,
                m.Role,
                ListTitle = m.List.Title,
                NotStartedCount = m.List.Items.Count(i => i.StatusId == (int)ItemStatus.NotStarted),
                PartialCount    = m.List.Items.Count(i => i.StatusId == (int)ItemStatus.Partial),
                CompleteCount   = m.List.Items.Count(i => i.StatusId == (int)ItemStatus.Complete),
                AbandonedCount  = m.List.Items.Count(i => i.StatusId == (int)ItemStatus.Abandoned),
                MemberCount = m.List.Members.Count,
                ListCreatedAt = m.List.CreatedAt,
                ListUpdatedAt = m.List.UpdatedAt,
                ListClosedAt = m.List.ClosedAt,
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException();

        var ownerName = await db.ListMembers
            .Where(m => m.ListId == request.ListId && m.Role == MemberRole.Owner)
            .Select(m => m.Profile.DisplayName)
            .FirstOrDefaultAsync(ct) ?? "Unknown";

        var contributorNames = await db.ListMembers
            .Where(m => m.ListId == request.ListId && m.Role == MemberRole.Contributor)
            .Select(m => m.Profile.DisplayName)
            .ToListAsync(ct);

        var tags = await db.TodoItemTags
            .Where(t => t.Item.ListId == request.ListId)
            .Select(t => new { t.Tag.Id, t.Tag.Name, t.Tag.Color })
            .Distinct()
            .Select(t => new TagDto(t.Id, t.Name, t.Color))
            .ToListAsync(ct);

        return new TodoListResponse(
            membership.ListId,
            membership.ListTitle,
            membership.Role,
            ownerName,
            contributorNames,
            membership.MemberCount,
            membership.NotStartedCount,
            membership.PartialCount,
            membership.CompleteCount,
            membership.AbandonedCount,
            membership.ListCreatedAt,
            membership.ListUpdatedAt,
            membership.ListClosedAt,
            tags);
    }
}
