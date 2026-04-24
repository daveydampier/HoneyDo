namespace HoneyDo.Common.Services;

public interface IActivityLogger
{
    /// <summary>
    /// Stages an activity log entry on the current DbContext change tracker.
    /// The entry is committed when the handler calls SaveChangesAsync — no separate
    /// save is performed here so the log entry and the domain change are always atomic.
    /// </summary>
    void Log(Guid listId, Guid actorId, string actionType, string? detail = null, DateTime? timestamp = null);
}
