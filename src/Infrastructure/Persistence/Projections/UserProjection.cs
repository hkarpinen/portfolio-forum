using Forum.Domain.ValueObjects;

namespace Infrastructure.Persistence.Projections;

/// <summary>
/// Denormalized projection of user data synced from the Identity service via domain events.
/// Persisted by infrastructure event consumers; read by infrastructure query classes.
/// Not a domain aggregate — has no lifecycle, invariants, or domain events.
/// </summary>
public sealed class UserProjection
{
    public UserId Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }

    /// <summary>Returns DisplayName if set, otherwise falls back to UserName.</summary>
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
