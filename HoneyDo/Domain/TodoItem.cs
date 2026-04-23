namespace HoneyDo.Domain;

public class TodoItem
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public Guid? AssignedToId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string? Notes { get; set; }
    public string? DueDate { get; set; }
    public bool IsStarred { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public TodoList List { get; set; } = null!;
    public TaskStatus Status { get; set; } = null!;
    public Profile? AssignedTo { get; set; }
    public ICollection<TodoItemTag> ItemTags { get; set; } = [];
}
