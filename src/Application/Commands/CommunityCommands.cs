using Forum.Domain.ValueObjects;

namespace Forum.Application.Commands;

public sealed record CreateCommunityCommand(string Name, CommunityVisibility Visibility, string? Description = null, string? ImageUrl = null, string? Rules = null, Guid OwnerId = default);
public sealed record UpdateCommunityCommand(string Name, CommunityVisibility Visibility, string? Description = null, string? ImageUrl = null, string? Rules = null, Guid CommunityId = default, Guid RequestingUserId = default, bool RequestingUserIsAdmin = false);
public sealed record CommunityBySlugCommand(string Slug);
public sealed record TransferCommunityOwnershipCommand(Guid NewOwnerId, Guid CommunityId = default);
public sealed record DeleteCommunityCommand(Guid CommunityId = default, Guid RequestedByUserId = default, bool RequestingUserIsAdmin = false);
/// <summary>A non-null <paramref name="MembershipUserId"/> filters to that user's own
/// communities, so a caller needn't fetch all of them and intersect.</summary>
public sealed record ListCommunitiesCommand(int Page = 1, int PageSize = 20, Guid? MembershipUserId = null);
public sealed record CommunityDetailCommand(Guid CommunityId);
