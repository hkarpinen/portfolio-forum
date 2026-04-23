using Forum.Domain.ValueObjects;

namespace Forum.Domain.ReadModels;

/// <summary>
/// Denormalized read model for user data projected from the Identity service.
/// Not an aggregate — has no lifecycle, invariants, or domain events.
/// </summary>
public sealed class UserProjection
{
    public UserId Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime RegisteredAt { get; set; }
    public bool IsBanned { get; set; }

    public UserProjection() { }

    public UserProjection(UserId id, string userName, string? displayName, string? avatarUrl, DateTime registeredAt, bool isBanned)
    {
        Id = id;
        UserName = userName;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        RegisteredAt = registeredAt;
        IsBanned = isBanned;
    }
}
