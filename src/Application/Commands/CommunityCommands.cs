using Forum.Domain.ValueObjects;

namespace Forum.Application.Commands;

public sealed record CreateCommunityCommand(string Name, CommunityVisibility Visibility, string? Description = null, string? ImageUrl = null, string? Rules = null, Guid OwnerId = default);
public sealed record UpdateCommunityCommand(string Name, CommunityVisibility Visibility, string? Description = null, string? ImageUrl = null, string? Rules = null, Guid CommunityId = default, Guid RequestingUserId = default, bool RequestingUserIsAdmin = false);
public sealed record CommunityBySlugCommand(string Slug);
public sealed record TransferCommunityOwnershipCommand(Guid NewOwnerId, Guid CommunityId = default);
public sealed record DeleteCommunityCommand(Guid CommunityId = default, Guid RequestedByUserId = default, bool RequestingUserIsAdmin = false);
/// <summary>
/// List communities. When <paramref name="MembershipUserId"/> is non-null,
/// the result is filtered to communities the given user is a member of —
/// powers the "Your communities" sidebar on the forum landing page in one
/// round-trip instead of (all communities + memberships + client filter).
/// </summary>
public sealed record ListCommunitiesCommand(int Page = 1, int PageSize = 20, Guid? MembershipUserId = null);
public sealed record CommunityDetailCommand(Guid CommunityId);
