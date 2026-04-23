using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record ThreadEdited(
    ThreadId ThreadId,
    string Title,
    string? Content,
    DateTime EditedAt
);
