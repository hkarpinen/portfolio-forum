using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record ThreadCreated(
    ThreadId ThreadId,
    CommunityId CommunityId,
    string CommunitySlug,
    UserId AuthorId,
    string Title,
    DateTime CreatedAt
);
