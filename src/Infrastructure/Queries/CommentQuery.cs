using Forum.Application.Contracts;
using Forum.Application.Queries;
using Forum.Domain.Aggregates;
using Forum.Domain.ReadModels;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Queries;

internal sealed class CommentQuery : ICommentQuery
{
    private readonly ForumDbContext _db;

    public CommentQuery(ForumDbContext db) => _db = db;

    public async Task<CommentTreeResponse> ListTreeAsync(ListCommentTreeRequest request, CancellationToken cancellationToken = default)
    {
        var threadId = new ThreadId(request.ThreadId);
        var comments = await _db.Comments
            .Where(c => c.ThreadId == threadId && c.DeletedAt == null)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var authorIds = comments.Select(c => c.AuthorId).Distinct().ToList();
        var projections = await _db.UserProjections
            .Where(p => authorIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var projDict = projections.ToDictionary(p => p.Id);

        var nodes = comments.Select(c =>
        {
            projDict.TryGetValue(c.AuthorId, out var proj);
            return new CommentTreeNodeResponse(Map(c, null, proj), Array.Empty<CommentTreeNodeResponse>());
        }).ToArray();

        return new CommentTreeResponse(nodes);
    }

    private static CommentResponse Map(Comment comment, Guid? parentCommentId, UserProjection? proj) => new(
        comment.Id.Value,
        comment.ThreadId.Value,
        comment.AuthorId.Value,
        proj?.DisplayName ?? proj?.UserName,
        proj?.AvatarUrl,
        comment.Content,
        comment.CreatedAt,
        comment.EditedAt,
        comment.DeletedAt,
        parentCommentId);
}
