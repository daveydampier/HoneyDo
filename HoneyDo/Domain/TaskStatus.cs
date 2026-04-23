namespace HoneyDo.Domain;

public class TaskStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<TodoItem> Items { get; set; } = [];
}
