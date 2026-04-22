using HoneyDo.Data;
using HoneyDo.Domain;
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
    int ItemCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ClosedAt);

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
                ItemCount = m.List.Items.Count,
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

        return memberships.Select(m => new TodoListResponse(
            m.ListId,
            m.ListTitle,
            m.Role,
            ownerNames.GetValueOrDefault(m.ListId) ?? "Unknown",
            m.MemberCount,
            m.ItemCount,
            m.ListCreatedAt,
            m.ListUpdatedAt,
            m.ListClosedAt));
    }
}
