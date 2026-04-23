using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record VoteCast(
    VoteId VoteId,
    VoteTargetType TargetType,
    Guid TargetId,
    UserId UserId,
    VoteDirection Direction,
    DateTime CastAt
);
