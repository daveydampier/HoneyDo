namespace HoneyDo.Domain;

public class TodoItemTag
{
    public Guid ItemId { get; set; }
    public Guid TagId { get; set; }

    public TodoItem Item { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
