namespace Forum.Application.Dtos;

public sealed record ForumProfileDto(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    string? Bio,
    string? Signature,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
