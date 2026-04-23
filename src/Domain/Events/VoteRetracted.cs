using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record VoteRetracted(
    VoteId VoteId,
    VoteTargetType TargetType,
    Guid TargetId,
    UserId UserId,
    DateTime RetractedAt
);
