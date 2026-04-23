using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record CommentCreated(
    CommentId CommentId,
    ThreadId ThreadId,
    UserId AuthorId,
    string Content,
    DateTime CreatedAt
);
