namespace HoneyDo.Domain;

public enum FriendStatus { Pending, Accepted, Blocked }

public class Friend
{
    public Guid RequesterId { get; set; }
    public Guid AddresseeId { get; set; }
    public FriendStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public Profile Requester { get; set; } = null!;
    public Profile Addressee { get; set; } = null!;
}
