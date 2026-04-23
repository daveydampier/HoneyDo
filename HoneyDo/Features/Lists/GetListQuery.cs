using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using HoneyDo.Domain;
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
                ItemCount = m.List.Items.Count,
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

        return new TodoListResponse(
            membership.ListId,
            membership.ListTitle,
            membership.Role,
            ownerName,
            membership.MemberCount,
            membership.ItemCount,
            membership.ListCreatedAt,
            membership.ListUpdatedAt,
            membership.ListClosedAt);
    }
}
