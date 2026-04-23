using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record ModerationActionLogged(
    LogId LogId,
    CommunityId CommunityId,
    ModerationAction Action,
    UserId PerformedBy,
    UserId? TargetUserId,
    string? TargetContent,
    DateTime PerformedAt
);
