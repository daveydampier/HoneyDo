namespace HoneyDo.Domain;

/// <summary>
/// Named constants for the <c>TaskStatus</c> lookup-table rows stored in <c>TodoItem.StatusId</c>.
/// Use <c>(int)ItemStatus.Complete</c> when comparing against the integer column.
/// The database stores the underlying int; no migration is needed when adding this enum.
/// </summary>
public enum ItemStatus
{
    NotStarted = 1,
    Partial    = 2,
    Complete   = 3,
    Abandoned  = 4,
}

public static class ItemStatusExtensions
{
    /// <summary>Returns the human-readable display name for the status (e.g. "Not Started").</summary>
    public static string ToDisplayName(this ItemStatus status) => status switch
    {
        ItemStatus.NotStarted => "Not Started",
        ItemStatus.Partial    => "Partial",
        ItemStatus.Complete   => "Complete",
        ItemStatus.Abandoned  => "Abandoned",
        _                     => "Unknown"
    };
}
