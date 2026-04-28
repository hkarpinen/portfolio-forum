using Forum.Application.Contracts;
using Forum.Application.Queries;
using Forum.Domain.Engines;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Queries;

internal sealed class ThreadQuery : IThreadQuery
{
    private readonly ForumDbContext _db;
    private readonly IHotRankingEngine _hotRankingEngine;

    public ThreadQuery(ForumDbContext db, IHotRankingEngine hotRankingEngine)
    {
        _db = db;
        _hotRankingEngine = hotRankingEngine;
    }

    public async Task<ThreadListResponse> ListAsync(ListThreadsRequest request, CancellationToken cancellationToken = default)
    {
        var communityId = new CommunityId(request.CommunityId);
        var query = _db.Threads.Where(t => t.CommunityId == communityId && t.DeletedAt == null);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var authorIds = items.Select(t => t.AuthorId).Distinct().ToList();
        var projections = await _db.UserProjections
            .Where(p => authorIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var projDict = projections.ToDictionary(p => p.Id);

        var responses = items.Select(t =>
        {
            projDict.TryGetValue(t.AuthorId, out var proj);
            var hotScore = _hotRankingEngine.CalculateHotScore(t.CreatedAt, t.VoteScore, 0);
            return new ThreadSummaryResponse(
                t.Id.Value, t.CommunityId.Value, t.AuthorId.Value,
                proj?.EffectiveName, proj?.AvatarUrl, t.Title,
                t.CreatedAt, hotScore, t.VoteScore);
        }).ToList();

        return new ThreadListResponse(responses, total);
    }

    public async Task<ThreadResponse?> GetDetailAsync(ThreadDetailRequest request, CancellationToken cancellationToken = default)
    {
        var thread = await _db.Threads.FirstOrDefaultAsync(t => t.Id == new ThreadId(request.ThreadId), cancellationToken);
        if (thread is null) return null;

        var proj = await _db.UserProjections.FirstOrDefaultAsync(p => p.Id == thread.AuthorId, cancellationToken);
        var hotScore = _hotRankingEngine.CalculateHotScore(thread.CreatedAt, thread.VoteScore, 0);
        return new ThreadResponse(
            thread.Id.Value, thread.CommunityId.Value, thread.AuthorId.Value,
            proj?.EffectiveName, proj?.AvatarUrl, thread.Title, thread.Content,
            thread.CreatedAt, thread.EditedAt, thread.IsLocked, thread.IsPinned,
            thread.DeletedAt, hotScore, thread.VoteScore);
    }

    public async Task<ThreadListResponse> ListByAuthorAsync(Guid authorId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var authorUserId = new UserId(authorId);
        var query = _db.Threads.Where(t => t.AuthorId == authorUserId && t.DeletedAt == null);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var proj = await _db.UserProjections.FirstOrDefaultAsync(p => p.Id == authorUserId, cancellationToken);

        var responses = items.Select(t =>
        {
            var hotScore = _hotRankingEngine.CalculateHotScore(t.CreatedAt, t.VoteScore, 0);
            return new ThreadSummaryResponse(
                t.Id.Value, t.CommunityId.Value, t.AuthorId.Value,
                proj?.EffectiveName, proj?.AvatarUrl, t.Title,
                t.CreatedAt, hotScore, t.VoteScore);
        }).ToList();
        return new ThreadListResponse(responses, total);
    }
}
