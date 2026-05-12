namespace Forum.Application.Commands;

public sealed record JoinCommunityCommand(Guid CommunityId, Guid UserId);
public sealed record InviteMemberCommand(Guid CommunityId, Guid UserId, Guid InvitedByUserId = default);
public sealed record LeaveCommunityCommand(Guid MembershipId);
public sealed record AppointModeratorCommand(Guid MembershipId);
public sealed record RemoveModeratorCommand(Guid MembershipId);
