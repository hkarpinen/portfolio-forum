using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record MembershipJoined(
    MembershipId MembershipId,
    CommunityId CommunityId,
    UserId UserId,
    DateTime JoinedAt
);
