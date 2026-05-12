namespace Forum.Application.Commands;

public sealed record CreateThreadCommand(string CommunitySlug, string Title, string? Content, string? Flair = null, Guid CommunityId = default, Guid AuthorId = default);
public sealed record EditThreadCommand(string Title, string? Content, string? Flair = null, Guid ThreadId = default);
public sealed record DeleteThreadCommand(Guid ThreadId = default);
public sealed record LockThreadCommand(Guid ThreadId = default);
public sealed record PinThreadCommand(Guid ThreadId = default);
public sealed record ListThreadsCommand(Guid CommunityId, int Page = 1, int PageSize = 20);
public sealed record ThreadDetailCommand(Guid ThreadId);
public sealed record FeedCommand(string Sort = "new", int Page = 1, int PageSize = 20);
