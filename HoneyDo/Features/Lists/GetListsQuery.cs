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
    IEnumerable<string> ContributorNames,
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
                NotStartedCount = m.List.Items.Count(i => i.StatusId == (int)ItemStatus.NotStarted),
                PartialCount    = m.List.Items.Count(i => i.StatusId == (int)ItemStatus.Partial),
                CompleteCount   = m.List.Items.Count(i => i.StatusId == (int)ItemStatus.Complete),
                AbandonedCount  = m.List.Items.Count(i => i.StatusId == (int)ItemStatus.Abandoned),
                MemberCount = m.List.Members.Count,
                ListCreatedAt = m.List.CreatedAt,
                ListUpdatedAt = m.List.UpdatedAt,
                ListClosedAt = m.List.ClosedAt,
            })
            .ToListAsync(ct);

        var listIds = memberships.Select(m => m.ListId).ToList();

        // Separate query to reliably resolve member DisplayNames via Profiles join
        var memberNames = await db.ListMembers
            .Where(m => listIds.Contains(m.ListId))
            .Select(m => new { m.ListId, m.Role, m.Profile.DisplayName })
            .ToListAsync(ct);

        var ownerNames = memberNames
            .Where(m => m.Role == MemberRole.Owner)
            .ToDictionary(m => m.ListId, m => m.DisplayName);

        var contributorsByList = memberNames
            .Where(m => m.Role == MemberRole.Contributor)
            .GroupBy(m => m.ListId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.DisplayName).ToList());

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
            contributorsByList.GetValueOrDefault(m.ListId) ?? [],
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
