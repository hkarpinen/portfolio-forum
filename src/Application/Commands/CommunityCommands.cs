using Forum.Domain.ValueObjects;

namespace Forum.Application.Commands;

public sealed record CreateCommunityCommand(string Name, CommunityVisibility Visibility, string? Description = null, string? ImageUrl = null, Guid OwnerId = default);
public sealed record UpdateCommunityCommand(string Name, CommunityVisibility Visibility, string? Description = null, string? ImageUrl = null, Guid CommunityId = default, Guid RequestingUserId = default, bool RequestingUserIsAdmin = false);
public sealed record CommunityBySlugCommand(string Slug);
public sealed record TransferCommunityOwnershipCommand(Guid NewOwnerId, Guid CommunityId = default);
public sealed record DeleteCommunityCommand(Guid CommunityId = default, Guid RequestedByUserId = default, bool RequestingUserIsAdmin = false);
public sealed record ListCommunitiesCommand(int Page = 1, int PageSize = 20);
public sealed record CommunityDetailCommand(Guid CommunityId);
