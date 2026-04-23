namespace HoneyDo.Domain;

public class TodoList
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<ListMember> Members { get; set; } = [];
    public ICollection<TodoItem> Items { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
}
