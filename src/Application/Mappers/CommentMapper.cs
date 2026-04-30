using Forum.Application.Contracts;
using Forum.Domain.ReadModels;

namespace Forum.Application.Mappers;

public static class CommentMapper
{
    public static CommentResponse ToResponse(
        Forum.Domain.Aggregates.Comment comment,
        UserProjection? proj,
        int voteScore) => new(
        comment.Id.Value,
        comment.ThreadId.Value,
        comment.AuthorId.Value,
        proj?.EffectiveName,
        proj?.AvatarUrl,
        comment.Content,
        comment.CreatedAt,
        comment.EditedAt,
        comment.DeletedAt,
        comment.ParentCommentId?.Value,
        voteScore);
}
