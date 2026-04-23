using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record CommentDeleted(
    CommentId CommentId,
    DateTime DeletedAt
);
