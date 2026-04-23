using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record MembershipInvited(
    MembershipId MembershipId,
    CommunityId CommunityId,
    UserId InvitedUserId,
    UserId InvitedByUserId,
    DateTime InvitedAt
);
