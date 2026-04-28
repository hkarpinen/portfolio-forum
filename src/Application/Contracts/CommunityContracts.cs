using Forum.Domain.ValueObjects;

namespace Forum.Application.Contracts;

public sealed record CreateCommunityRequest(string Name, CommunityVisibility Visibility, Guid OwnerId, string? Description = null, string? ImageUrl = null);
public sealed record UpdateCommunityRequest(Guid CommunityId, string Name, CommunityVisibility Visibility, Guid RequestingUserId, bool RequestingUserIsAdmin, string? Description = null, string? ImageUrl = null);
public sealed record CommunityBySlugRequest(string Slug);
public sealed record TransferCommunityOwnershipRequest(Guid CommunityId, Guid NewOwnerId);
public sealed record DeleteCommunityRequest(Guid CommunityId, Guid RequestedByUserId, bool RequestingUserIsAdmin);
public sealed record ListCommunitiesRequest(int Page = 1, int PageSize = 20);
public sealed record CommunityDetailRequest(Guid CommunityId);

public sealed record CommunityActivitySnapshot(
    Guid ThreadId,
    string ThreadTitle,
    DateTime ThreadCreatedAt,
    double HotScore,
    string? AuthorDisplayName,
    string? AuthorAvatarUrl,
    DateTime? LatestReplyAt,
    string? LatestReplyAuthorDisplayName,
    string? LatestReplyAuthorAvatarUrl);

public sealed record CommunityResponse(
    Guid CommunityId,
    string Slug,
    string Name,
    string? Description,
    string? ImageUrl,
    CommunityVisibility Visibility,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    CommunityActivitySnapshot? LatestActivity = null,
    int MemberCount = 0,
    int ThreadCount = 0,
    int CommentCount = 0);

public sealed record CommunityListResponse(IReadOnlyCollection<CommunityResponse> Items, int TotalCount);
