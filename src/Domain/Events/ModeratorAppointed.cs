using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record ModeratorAppointed(
    CommunityId CommunityId,
    UserId UserId,
    DateTime AppointedAt
);
