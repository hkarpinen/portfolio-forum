using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record MembershipLeft(
    MembershipId MembershipId,
    CommunityId CommunityId,
    UserId UserId,
    DateTime LeftAt
);
