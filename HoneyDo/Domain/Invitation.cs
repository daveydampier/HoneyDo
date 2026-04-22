namespace HoneyDo.Domain;

public class Invitation
{
    public Guid Id { get; set; }
    public Guid InviterId { get; set; }

    /// <summary>The email address that was invited (lowercase).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>URL-safe base64 token included in the registration link.</summary>
    public string Token { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }

    /// <summary>Set when the invitee completes registration via the invite link.</summary>
    public DateTime? AcceptedAt { get; set; }

    public Profile Inviter { get; set; } = null!;
}
