using Forum.Domain.ValueObjects;

namespace Forum.Application.Dtos;

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

public sealed record CommunityDto(
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

public sealed record CommunityListDto(IReadOnlyCollection<CommunityDto> Items, int TotalCount);
