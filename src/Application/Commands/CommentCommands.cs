namespace Forum.Application.Commands;

public sealed record CreateCommentCommand(Guid ThreadId, string Content, Guid? ParentCommentId = null, Guid AuthorId = default);
public sealed record EditCommentCommand(string Content, Guid CommentId = default);
public sealed record DeleteCommentCommand(Guid CommentId = default);
public sealed record ListCommentTreeCommand(Guid ThreadId, Guid? CallerId = null);
