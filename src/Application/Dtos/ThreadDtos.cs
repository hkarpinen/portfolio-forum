namespace Forum.Application.Dtos;

public sealed record ThreadDto(
    Guid ThreadId,
    Guid CommunityId,
    Guid AuthorId,
    string? AuthorDisplayName,
    string? AuthorAvatarUrl,
    string Title,
    string? Content,
    IReadOnlyList<string> Tags,
    DateTime CreatedAt,
    DateTime? EditedAt,
    bool IsLocked,
    bool IsPinned,
    DateTime? DeletedAt,
    double HotScore,
    int VoteScore,
    /// <summary>Null when not voted or signed out. Carries the <c>VoteId</c> that switch
    /// and retract are keyed by, which is otherwise only in the POST response.</summary>
    MyVoteDto? MyVote);

public sealed record MyVoteDto(Guid VoteId, int Direction);

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
    IReadOnlyList<string> Tags,
    DateTime CreatedAt,
    double HotScore,
    int VoteScore,
    int CommentCount,
    /// <summary>First ~160 characters of the body.</summary>
    string? Excerpt);

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
    IReadOnlyList<string> Tags,
    DateTime CreatedAt,
    double HotScore,
    int VoteScore,
    int CommentCount,
    bool IsPinned,
    string? Excerpt);

public sealed record FeedListDto(IReadOnlyCollection<FeedThreadSummaryDto> Items, int TotalCount);
