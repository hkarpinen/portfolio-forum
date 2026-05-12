using Forum.Domain.ValueObjects;

namespace Forum.Application.Dtos;

public sealed record MembershipDto(
    Guid MembershipId,
    Guid CommunityId,
    Guid UserId,
    CommunityRole Role,
    DateTime JoinedAt,
    bool IsInvite);
