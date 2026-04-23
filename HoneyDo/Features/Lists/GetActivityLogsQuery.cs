using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Lists;

public record GetActivityLogsQuery(Guid ListId, Guid ProfileId) : IRequest<IEnumerable<ActivityLogResponse>>;

public record ActivityLogResponse(
    Guid Id,
    string ActionType,
    string ActorName,
    DateTime Timestamp);

public class GetActivityLogsHandler(AppDbContext db) : IRequestHandler<GetActivityLogsQuery, IEnumerable<ActivityLogResponse>>
{
    public async Task<IEnumerable<ActivityLogResponse>> Handle(GetActivityLogsQuery request, CancellationToken ct)
    {
        var isMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct);

        if (!isMember) throw new NotFoundException();

        return await db.ActivityLogs
            .Where(a => a.ListId == request.ListId)
            .OrderByDescending(a => a.Timestamp)
            .Take(200)
            .Select(a => new ActivityLogResponse(
                a.Id,
                a.ActionType,
                a.Actor.DisplayName,
                a.Timestamp))
            .ToListAsync(ct);
    }
}
