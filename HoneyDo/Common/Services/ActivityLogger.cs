using HoneyDo.Data;

namespace HoneyDo.Common.Services;

public class ActivityLogger(AppDbContext db) : IActivityLogger
{
    public void Log(Guid listId, Guid actorId, string actionType, string? detail = null, DateTime? timestamp = null)
    {
        db.ActivityLogs.Add(new Domain.ActivityLog
        {
            Id        = Guid.NewGuid(),
            ListId    = listId,
            ActorId   = actorId,
            ActionType = actionType,
            Detail    = detail,
            Timestamp = timestamp ?? DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Truncates <paramref name="s"/> to at most <paramref name="maxLength"/> characters,
    /// appending an ellipsis when truncation occurs. Use this when building Detail strings
    /// that include user-supplied content of unbounded length.
    /// </summary>
    public static string Truncate(string s, int maxLength = 100) =>
        s.Length > maxLength ? s[..(maxLength - 1)] + "…" : s;
}
