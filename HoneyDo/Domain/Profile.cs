namespace HoneyDo.Domain;

public class Profile
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<ListMember> ListMemberships { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
    public ICollection<Friend> SentRequests { get; set; } = [];
    public ICollection<Friend> ReceivedRequests { get; set; } = [];
}
