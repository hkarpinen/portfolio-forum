using Forum.Application.Contracts;
using Forum.Domain.Aggregates;

namespace Forum.Application.Mappers;

public static class ForumMembershipMapper
{
    public static MembershipResponse ToResponse(CommunityMembership membership, bool isInvite) => new(
        membership.Id.Value,
        membership.CommunityId.Value,
        membership.UserId.Value,
        membership.Role,
        membership.JoinedAt,
        isInvite);
}
