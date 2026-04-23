using Forum.Domain.ValueObjects;

namespace Forum.Application.Contracts;

public sealed record JoinCommunityRequest(Guid CommunityId, Guid UserId);
public sealed record InviteMemberRequest(Guid CommunityId, Guid InvitedByUserId, Guid UserId);
public sealed record LeaveCommunityRequest(Guid MembershipId);
public sealed record AppointModeratorRequest(Guid MembershipId);
public sealed record RemoveModeratorRequest(Guid MembershipId);

public sealed record MembershipResponse(
    Guid MembershipId,
    Guid CommunityId,
    Guid UserId,
    CommunityRole Role,
    DateTime JoinedAt,
    DateTime? LeftAt,
    bool IsInvite);
