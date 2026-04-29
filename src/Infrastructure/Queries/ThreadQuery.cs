using Forum.Application.Contracts;
using Forum.Application.Queries;
using Forum.Domain.Aggregates;
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

    public async Task<FeedListResponse> ListFeedAsync(FeedRequest request, CancellationToken cancellationToken = default)
    {
        var baseQuery = _db.Threads.Where(t => t.DeletedAt == null);
        var total = await baseQuery.CountAsync(cancellationToken);

        List<ForumThread> candidates;
        if (request.Sort == "hot")
        {
            // Fetch a wider window so the in-memory hot sort has enough material to page into
            candidates = await baseQuery
                .OrderByDescending(t => t.CreatedAt)
                .Take(Math.Max(500, request.Page * request.PageSize * 4))
                .ToListAsync(cancellationToken);
        }
        else
        {
            candidates = await baseQuery
                .OrderByDescending(t => t.CreatedAt)
                .Take(Math.Max(500, request.Page * request.PageSize * 4))
                .ToListAsync(cancellationToken);
        }

        // Load comment counts for all candidates — needed for correct hot scoring
        var candidateIds = candidates.Select(t => t.Id).ToList();
        var commentCounts = await _db.Comments
            .Where(c => candidateIds.Contains(c.ThreadId) && c.DeletedAt == null)
            .GroupBy(c => c.ThreadId)
            .Select(g => new { ThreadId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var commentCountMap = commentCounts.ToDictionary(x => x.ThreadId, x => x.Count);

        // Now sort with real comment counts feeding the hot score
        List<ForumThread> items;
        if (request.Sort == "hot")
        {
            items = candidates
                .OrderByDescending(t =>
                {
                    commentCountMap.TryGetValue(t.Id, out var cc);
                    return _hotRankingEngine.CalculateHotScore(t.CreatedAt, t.VoteScore, cc);
                })
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
        }
        else
        {
            items = candidates
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
        }

        // Load community info
        var communityIds = items.Select(t => t.CommunityId).Distinct().ToList();
        var communities = await _db.Communities
            .Where(c => communityIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        var communityMap = communities.ToDictionary(c => c.Id);

        // Load author projections
        var authorIds = items.Select(t => t.AuthorId).Distinct().ToList();
        var projections = await _db.UserProjections
            .Where(p => authorIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var projDict = projections.ToDictionary(p => p.Id);

        var responses = items.Select(t =>
        {
            projDict.TryGetValue(t.AuthorId, out var proj);
            communityMap.TryGetValue(t.CommunityId, out var community);
            commentCountMap.TryGetValue(t.Id, out var commentCount);
            var hotScore = _hotRankingEngine.CalculateHotScore(t.CreatedAt, t.VoteScore, commentCount);
            return new FeedThreadSummaryResponse(
                t.Id.Value, t.CommunityId.Value,
                community?.Slug, community?.Name,
                t.AuthorId.Value,
                proj?.EffectiveName, proj?.AvatarUrl, t.Title,
                t.CreatedAt, hotScore, t.VoteScore, commentCount, t.IsPinned);
        }).ToList();

        return new FeedListResponse(responses, total);
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
