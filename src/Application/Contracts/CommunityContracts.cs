using Forum.Domain.ValueObjects;

namespace Forum.Application.Contracts;

public sealed record CreateCommunityRequest(string Name, CommunityVisibility Visibility, Guid OwnerId, string? Description = null, string? ImageUrl = null);
public sealed record UpdateCommunityRequest(Guid CommunityId, string Name, CommunityVisibility Visibility, string? Description = null, string? ImageUrl = null);
public sealed record CommunityByNameRequest(string Name);
public sealed record TransferCommunityOwnershipRequest(Guid CommunityId, Guid NewOwnerId);
public sealed record ListCommunitiesRequest(int Page = 1, int PageSize = 20);
public sealed record CommunityDetailRequest(Guid CommunityId);

public sealed record CommunityResponse(
    Guid CommunityId,
    string Name,
    string? Description,
    string? ImageUrl,
    CommunityVisibility Visibility,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CommunityListResponse(IReadOnlyCollection<CommunityResponse> Items, int TotalCount);
