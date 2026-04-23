using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record ModeratorRemoved(
    CommunityId CommunityId,
    UserId UserId,
    DateTime RemovedAt
);
