using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record ThreadPinned(
    ThreadId ThreadId,
    DateTime PinnedAt
);
