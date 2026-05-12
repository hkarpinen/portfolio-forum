using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Mappers;
using Forum.Application.Queries;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Queries;

internal sealed class CommentQuery : ICommentQuery
{
    private readonly ForumDbContext _db;

    public CommentQuery(ForumDbContext db) => _db = db;

    public async Task<CommentTreeDto> ListTreeAsync(ListCommentTreeCommand request, CancellationToken cancellationToken = default)
    {
        var threadId = new ThreadId(request.ThreadId);
        var comments = await _db.Comments
            .AsNoTracking()
            .Where(c => c.ThreadId == threadId && c.DeletedAt == null)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var authorIds = comments.Select(c => c.AuthorId).Distinct().ToList();
        var projections = await _db.UserProjections
            .AsNoTracking()
            .Where(p => authorIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var projDict = projections.ToDictionary(p => p.Id);

        // Build a mutable node map keyed by CommentId — VoteScore read from column, no votes aggregation
        var nodeMap = comments.ToDictionary(
            c => c.Id.Value,
            c =>
            {
                projDict.TryGetValue(c.AuthorId, out var proj);
                return (response: CommentMapper.ToDto(c, proj?.EffectiveName, proj?.AvatarUrl, c.VoteScore), children: new List<CommentTreeNodeDto>());
            });

        // Wire children into their parents; collect root-level comments
        var roots = new List<CommentTreeNodeDto>();
        foreach (var comment in comments)
        {
            var parentId = comment.ParentCommentId?.Value;
            if (parentId.HasValue && nodeMap.TryGetValue(parentId.Value, out var parent))
            {
                parent.children.Add(new CommentTreeNodeDto(nodeMap[comment.Id.Value].response, nodeMap[comment.Id.Value].children));
            }
            else
            {
                roots.Add(new CommentTreeNodeDto(nodeMap[comment.Id.Value].response, nodeMap[comment.Id.Value].children));
            }
        }

        return new CommentTreeDto(roots);
    }

    public async Task<ProfileCommentListDto> ListByAuthorAsync(Guid authorId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var authorIdVO = new UserId(authorId);
        var total = await _db.Comments
            .CountAsync(c => c.AuthorId == authorIdVO && c.DeletedAt == null, cancellationToken);

        var comments = await _db.Comments
            .AsNoTracking()
            .Where(c => c.AuthorId == authorIdVO && c.DeletedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (comments.Count == 0)
            return new ProfileCommentListDto([], total);

        var threadIds = comments.Select(c => c.ThreadId).Distinct().ToList();
        var threads = await _db.Threads
            .AsNoTracking()
            .Where(t => threadIds.Contains(t.Id) && t.DeletedAt == null)
            .ToListAsync(cancellationToken);
        var threadDict = threads.ToDictionary(t => t.Id.Value);

        var communityIds = threads.Select(t => t.CommunityId).Distinct().ToList();
        var communities = await _db.Communities
            .AsNoTracking()
            .Where(c => communityIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        var communityDict = communities.ToDictionary(c => c.Id.Value);

        var items = comments
            .Where(c => threadDict.ContainsKey(c.ThreadId.Value))
            .Select(c =>
            {
                var thread = threadDict[c.ThreadId.Value];
                communityDict.TryGetValue(thread.CommunityId.Value, out var comm);
                return new ProfileCommentSummaryDto(
                    c.Id.Value,
                    c.ThreadId.Value,
                    thread.Title,
                    comm?.Slug ?? "",
                    comm?.Name ?? "",
                    c.Content.Length > 200 ? c.Content[..200] + "…" : c.Content,
                    c.CreatedAt,
                    c.VoteScore);
            })
            .ToList();

        return new ProfileCommentListDto(items, total);
    }
}
