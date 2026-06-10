namespace Forum.Application.Commands;

public sealed record CreateThreadCommand(string CommunitySlug, string Title, string? Content, IReadOnlyList<string>? Tags = null, Guid CommunityId = default, Guid AuthorId = default);
public sealed record EditThreadCommand(string Title, string? Content, IReadOnlyList<string>? Tags = null, Guid ThreadId = default);
public sealed record DeleteThreadCommand(Guid ThreadId = default);
public sealed record LockThreadCommand(Guid ThreadId = default);
public sealed record PinThreadCommand(Guid ThreadId = default);
public sealed record ListThreadsCommand(Guid CommunityId, int Page = 1, int PageSize = 20);
public sealed record ThreadDetailCommand(Guid ThreadId);
public sealed record FeedCommand(string Sort = "new", int Page = 1, int PageSize = 20);

// ── Draft authoring lifecycle ────────────────────────────────────────────────
// A draft IS a ForumThread in `ThreadStatus.Draft`. These commands drive
// the authoring workflow that culminates in `PublishDraftCommand`, at which
// point the existing public-thread surface (`/feed`, community listings,
// search) starts including it.

public sealed record BeginDraftCommand(string CommunitySlug, string? Title, string? Content, IReadOnlyList<string>? Tags = null, Guid CommunityId = default, Guid AuthorId = default);
public sealed record ReviseDraftCommand(string? Title, string? Content, IReadOnlyList<string>? Tags = null, Guid ThreadId = default, Guid AuthorId = default);
public sealed record AbandonDraftCommand(Guid ThreadId = default, Guid AuthorId = default);
public sealed record PublishDraftCommand(Guid ThreadId = default, Guid AuthorId = default);
