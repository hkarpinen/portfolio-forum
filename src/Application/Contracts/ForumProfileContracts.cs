namespace Forum.Application.Contracts;

public sealed record UpdateForumProfileRequest(Guid UserId, string? Bio, string? Signature);
public sealed record GetForumProfileRequest(Guid UserId);

public sealed record ForumProfileResponse(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    string? Bio,
    string? Signature,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
