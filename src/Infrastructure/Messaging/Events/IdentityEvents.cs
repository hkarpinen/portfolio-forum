// Wire contracts for identity events consumed from RabbitMQ.
// Namespace and type names MUST match the domain events published by the identity service
// so MassTransit routes to the same exchange (e.g. Domain.Events:UserRegistered).
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
