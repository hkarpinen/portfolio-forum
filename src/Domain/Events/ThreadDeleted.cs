using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record ThreadDeleted(
    ThreadId ThreadId,
    DateTime DeletedAt
);
