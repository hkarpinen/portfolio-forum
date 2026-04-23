using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record CommunityDeleted(
    CommunityId CommunityId,
    DateTime DeletedAt
);
