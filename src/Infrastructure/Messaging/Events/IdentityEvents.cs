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

// Identity raises this INSTEAD of UserRegistered for a demo account. Forum consumed only
// UserRegistered, so a demo user never got a projection row here and every thread they wrote
// rendered its author as "someone" — while finance and household, which do consume it, were fine.
public sealed record DemoUserCreated(
    Guid Id,
    DateTime OccurredAt,
    Guid UserId,
    string Email,
    string DisplayName,
    DateTime DemoExpiresAt);
