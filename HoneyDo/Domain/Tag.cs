namespace HoneyDo.Domain;

public class Tag
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Profile Profile { get; set; } = null!;
    public ICollection<TodoItemTag> ItemTags { get; set; } = [];
}
