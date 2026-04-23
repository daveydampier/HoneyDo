namespace HoneyDo.Domain;

public enum MemberRole { Owner, Contributor }

public class ListMember
{
    public Guid ListId { get; set; }
    public Guid ProfileId { get; set; }
    public MemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }

    public TodoList List { get; set; } = null!;
    public Profile Profile { get; set; } = null!;
}
