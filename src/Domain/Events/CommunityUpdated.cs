using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record CommunityUpdated(
    CommunityId CommunityId,
    string Name,
    CommunityVisibility Visibility,
    DateTime UpdatedAt
);
