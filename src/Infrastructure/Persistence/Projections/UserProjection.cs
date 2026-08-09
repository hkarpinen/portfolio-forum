using Forum.Domain.ValueObjects;

namespace Infrastructure.Persistence.Projections;

/// <summary>A read-model projection, not an aggregate — no invariants, no events.</summary>
public sealed class UserProjection
{
    public UserId Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }

    public string EffectiveName => DisplayName ?? UserName;
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
