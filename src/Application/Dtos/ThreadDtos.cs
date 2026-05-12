namespace Forum.Application.Dtos;

public sealed record ThreadDto(
    Guid ThreadId,
    Guid CommunityId,
    Guid AuthorId,
    string? AuthorDisplayName,
    string? AuthorAvatarUrl,
    string Title,
    string? Content,
    string? Flair,
    DateTime CreatedAt,
    DateTime? EditedAt,
    bool IsLocked,
    bool IsPinned,
    DateTime? DeletedAt,
    double HotScore,
    int VoteScore);

public sealed record ThreadMutationDto(
    Guid ThreadId,
    bool IsLocked,
    bool IsPinned,
    DateTime? EditedAt,
    DateTime? DeletedAt);

public sealed record ThreadSummaryDto(
    Guid ThreadId,
    Guid CommunityId,
    Guid AuthorId,
    string? AuthorDisplayName,
    string? AuthorAvatarUrl,
    string Title,
    string? Flair,
    DateTime CreatedAt,
    double HotScore,
    int VoteScore);

public sealed record ThreadListDto(IReadOnlyCollection<ThreadSummaryDto> Items, int TotalCount);

public sealed record FeedThreadSummaryDto(
    Guid ThreadId,
    Guid CommunityId,
    string? CommunitySlug,
    string? CommunityName,
    Guid AuthorId,
    string? AuthorDisplayName,
    string? AuthorAvatarUrl,
    string Title,
    string? Flair,
    DateTime CreatedAt,
    double HotScore,
    int VoteScore,
    int CommentCount,
    bool IsPinned);

public sealed record FeedListDto(IReadOnlyCollection<FeedThreadSummaryDto> Items, int TotalCount);
