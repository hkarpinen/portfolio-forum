namespace Forum.Application.Contracts;

public sealed record CreateCommentRequest(Guid ThreadId, Guid AuthorId, string Content, Guid? ParentCommentId = null);
public sealed record EditCommentRequest(Guid CommentId, string Content);
public sealed record DeleteCommentRequest(Guid CommentId);
public sealed record ListCommentTreeRequest(Guid ThreadId);

public sealed record CommentResponse(
    Guid CommentId,
    Guid ThreadId,
    Guid AuthorId,
    string? AuthorDisplayName,
    string? AuthorAvatarUrl,
    string Content,
    DateTime CreatedAt,
    DateTime? EditedAt,
    DateTime? DeletedAt,
    Guid? ParentCommentId);

public sealed record CommentTreeNodeResponse(CommentResponse Comment, IReadOnlyCollection<CommentTreeNodeResponse> Children);
public sealed record CommentTreeResponse(IReadOnlyCollection<CommentTreeNodeResponse> RootComments);
