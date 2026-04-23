using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record ForumProfileUpdated(
    UserId UserId,
    string? Bio,
    string? Signature,
    DateTime OccurredAt
);
