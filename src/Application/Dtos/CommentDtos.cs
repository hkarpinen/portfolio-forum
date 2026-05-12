namespace Forum.Application.Dtos;

public sealed record CommentDto(
    Guid CommentId,
    Guid ThreadId,
    Guid AuthorId,
    string? AuthorDisplayName,
    string? AuthorAvatarUrl,
    string Content,
    DateTime CreatedAt,
    DateTime? EditedAt,
    DateTime? DeletedAt,
    Guid? ParentCommentId,
    int VoteScore);

public sealed record CommentTreeNodeDto(CommentDto Comment, IReadOnlyCollection<CommentTreeNodeDto> Children);
public sealed record CommentTreeDto(IReadOnlyCollection<CommentTreeNodeDto> RootComments);

public sealed record ProfileCommentSummaryDto(
    Guid CommentId,
    Guid ThreadId,
    string ThreadTitle,
    string CommunitySlug,
    string CommunityName,
    string Content,
    DateTime CreatedAt,
    int VoteScore);

public sealed record ProfileCommentListDto(IReadOnlyCollection<ProfileCommentSummaryDto> Items, int TotalCount);
