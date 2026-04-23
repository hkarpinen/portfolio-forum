using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record CommunityCreated(
    CommunityId CommunityId,
    string Name,
    CommunityVisibility Visibility,
    UserId OwnerId,
    DateTime CreatedAt
);
