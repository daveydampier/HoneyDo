namespace HoneyDo.Features.Friends;

public record FriendResponse(Guid ProfileId, string DisplayName, string Email, string? AvatarUrl);

public record ReceivedRequestResponse(Guid RequesterId, string DisplayName, string Email, string? AvatarUrl, DateTime CreatedAt);

public record SentRequestResponse(Guid AddresseeId, string DisplayName, string Email, string? AvatarUrl, DateTime CreatedAt);
