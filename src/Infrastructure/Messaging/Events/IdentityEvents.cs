// Namespace and type names MUST match the publisher's, or these bind a different
// exchange and every message is missed silently.
namespace Domain.Events;

public sealed record UserRegistered(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    string Email,
    string DisplayName);

public sealed record UserProfileUpdated(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    string DisplayName,
    string? AvatarUrl);

public sealed record UserBanned(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    DateTime BannedAt);
