using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record ThreadLocked(
    ThreadId ThreadId,
    DateTime LockedAt
);
