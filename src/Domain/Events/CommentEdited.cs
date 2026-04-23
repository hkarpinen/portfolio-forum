using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record CommentEdited(
    CommentId CommentId,
    string Content,
    DateTime EditedAt
);
