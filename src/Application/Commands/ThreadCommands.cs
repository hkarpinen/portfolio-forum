namespace Forum.Application.Commands;

public sealed record CreateThreadCommand(string CommunitySlug, string Title, string? Content, IReadOnlyList<string>? Tags = null, Guid CommunityId = default, Guid AuthorId = default);
public sealed record EditThreadCommand(string Title, string? Content, IReadOnlyList<string>? Tags = null, Guid ThreadId = default);
// `CallerId` is load-bearing: delete allows the author or a moderator of the
// thread's OWN community; lock and pin are moderators only.
public sealed record DeleteThreadCommand(Guid ThreadId = default, Guid CallerId = default);
public sealed record LockThreadCommand(Guid ThreadId = default, Guid CallerId = default);
public sealed record PinThreadCommand(Guid ThreadId = default, Guid CallerId = default);
/// <summary><paramref name="Sort"/>: "new", "hot" (recency-weighted) or "top" (raw score).</summary>
public sealed record ListThreadsCommand(Guid CommunityId, string Sort = "new", int Page = 1, int PageSize = 20);
public sealed record ThreadDetailCommand(Guid ThreadId, Guid? CallerId = null);
public sealed record FeedCommand(string Sort = "new", int Page = 1, int PageSize = 20);

// A draft IS a thread in Draft status — publishing is a transition, not a copy.

public sealed record BeginDraftCommand(string CommunitySlug, string? Title, string? Content, IReadOnlyList<string>? Tags = null, Guid CommunityId = default, Guid AuthorId = default);
public sealed record ReviseDraftCommand(string? Title, string? Content, IReadOnlyList<string>? Tags = null, Guid ThreadId = default, Guid AuthorId = default);
public sealed record AbandonDraftCommand(Guid ThreadId = default, Guid AuthorId = default);
public sealed record PublishDraftCommand(Guid ThreadId = default, Guid AuthorId = default);
