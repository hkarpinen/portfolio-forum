using Forum.Application.Dtos;

namespace Forum.Application.Mappers;

public static class CommentMapper
{
    public static CommentDto ToDto(
        Forum.Domain.Aggregates.Comment comment,
        string? authorName,
        string? authorAvatarUrl,
        int voteScore,
        MyVoteDto? myVote = null) => new(
        comment.Id.Value,
        comment.ThreadId.Value,
        comment.AuthorId.Value,
        authorName,
        authorAvatarUrl,
        comment.Content,
        comment.CreatedAt,
        comment.EditedAt,
        comment.DeletedAt,
        comment.ParentCommentId?.Value,
        voteScore,
        myVote);
}
