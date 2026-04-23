namespace HoneyDo.Domain;

public class ActivityLog
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public Guid ActorId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTime Timestamp { get; set; }

    public TodoList List { get; set; } = null!;
    public Profile Actor { get; set; } = null!;
}
