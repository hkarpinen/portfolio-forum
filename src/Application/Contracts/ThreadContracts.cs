namespace Forum.Application.Contracts;

public sealed record CreateThreadRequest(Guid CommunityId, Guid AuthorId, string Title, string? Content);
public sealed record EditThreadRequest(Guid ThreadId, string Title, string? Content);
public sealed record DeleteThreadRequest(Guid ThreadId);
public sealed record LockThreadRequest(Guid ThreadId);
public sealed record PinThreadRequest(Guid ThreadId);
public sealed record ListThreadsRequest(Guid CommunityId, int Page = 1, int PageSize = 20);
public sealed record ThreadDetailRequest(Guid ThreadId);

public sealed record ThreadResponse(
    Guid ThreadId,
    Guid CommunityId,
    Guid AuthorId,
    string? AuthorDisplayName,
    string? AuthorAvatarUrl,
    string Title,
    string? Content,
    DateTime CreatedAt,
    DateTime? EditedAt,
    bool IsLocked,
    bool IsPinned,
    DateTime? DeletedAt,
    double HotScore,
    int VoteScore);

public sealed record ThreadListResponse(IReadOnlyCollection<ThreadResponse> Items, int TotalCount);
