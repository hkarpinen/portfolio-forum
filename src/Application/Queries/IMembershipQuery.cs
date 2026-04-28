namespace Forum.Application.Queries;

public interface IMembershipQuery
{
    Task<bool> IsMemberAsync(Guid communityId, Guid userId, CancellationToken cancellationToken = default);
    Task<(bool IsMember, string? Role)> GetMembershipAsync(Guid communityId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommunityMemberItem>> ListByCommunityAsync(Guid communityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserCommunityItem>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record CommunityMemberItem(
    Guid MembershipId,
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    string Role,
    DateTime JoinedAt);

public sealed record UserCommunityItem(
    Guid MembershipId,
    Guid CommunityId,
    string CommunityName,
    string CommunitySlug,
    string? CommunityImageUrl,
    string Role,
    DateTime JoinedAt);
