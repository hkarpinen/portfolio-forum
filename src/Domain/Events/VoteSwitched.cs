using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record VoteSwitched(
    VoteId VoteId,
    VoteTargetType TargetType,
    Guid TargetId,
    UserId UserId,
    VoteDirection OldDirection,
    VoteDirection NewDirection,
    DateTime SwitchedAt
);
