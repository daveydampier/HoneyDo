using HoneyDo.Data;
using HoneyDo.Domain;
using HoneyDo.Features.Items;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Lists;

public record GetListsQuery(Guid ProfileId) : IRequest<IEnumerable<TodoListResponse>>;

public record TodoListResponse(
    Guid Id,
    string Title,
    MemberRole Role,
    string OwnerName,
    int MemberCount,
    int NotStartedCount,
    int PartialCount,
    int CompleteCount,
    int AbandonedCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ClosedAt,
    IEnumerable<TagDto> Tags);

public class GetListsQueryHandler(AppDbContext db) : IRequestHandler<GetListsQuery, IEnumerable<TodoListResponse>>
{
    public async Task<IEnumerable<TodoListResponse>> Handle(GetListsQuery request, CancellationToken ct)
    {
        var memberships = await db.ListMembers
            .Where(m => m.ProfileId == request.ProfileId)
            .Select(m => new
            {
                m.ListId,
                m.Role,
                ListTitle = m.List.Title,
                NotStartedCount = m.List.Items.Count(i => i.StatusId == 1),
                PartialCount    = m.List.Items.Count(i => i.StatusId == 2),
                CompleteCount   = m.List.Items.Count(i => i.StatusId == 3),
                AbandonedCount  = m.List.Items.Count(i => i.StatusId == 4),
                MemberCount = m.List.Members.Count,
                ListCreatedAt = m.List.CreatedAt,
                ListUpdatedAt = m.List.UpdatedAt,
                ListClosedAt = m.List.ClosedAt,
            })
            .ToListAsync(ct);

        var listIds = memberships.Select(m => m.ListId).ToList();

        // Separate query to reliably resolve owner DisplayName via Profiles join
        var ownerNames = await db.ListMembers
            .Where(m => listIds.Contains(m.ListId) && m.Role == MemberRole.Owner)
            .Select(m => new { m.ListId, m.Profile.DisplayName })
            .ToDictionaryAsync(m => m.ListId, m => m.DisplayName, ct);

        // Distinct tags applied to any item in each list
        var rawTags = await db.TodoItemTags
            .Where(t => listIds.Contains(t.Item.ListId))
            .Select(t => new { t.Item.ListId, t.Tag.Id, t.Tag.Name, t.Tag.Color })
            .Distinct()
            .ToListAsync(ct);

        var tagsByList = rawTags
            .GroupBy(t => t.ListId)
            .ToDictionary(g => g.Key, g => g.Select(t => new TagDto(t.Id, t.Name, t.Color)).ToList());

        return memberships.Select(m => new TodoListResponse(
            m.ListId,
            m.ListTitle,
            m.Role,
            ownerNames.GetValueOrDefault(m.ListId) ?? "Unknown",
            m.MemberCount,
            m.NotStartedCount,
            m.PartialCount,
            m.CompleteCount,
            m.AbandonedCount,
            m.ListCreatedAt,
            m.ListUpdatedAt,
            m.ListClosedAt,
            tagsByList.GetValueOrDefault(m.ListId) ?? []));
    }
}
