using Forum.Application.Commands;
using Forum.Application.Dtos;
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

    public ThreadQuery(ForumDbContext db) => _db = db;

    public async Task<ThreadListDto> ListAsync(ListThreadsCommand request, CancellationToken cancellationToken = default)
    {
        var communityId = new CommunityId(request.CommunityId);
        var query = _db.Threads.AsNoTracking().Where(t => t.CommunityId == communityId && t.DeletedAt == null);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var authorIds = items.Select(t => t.AuthorId).Distinct().ToList();
        var projections = await _db.UserProjections
            .AsNoTracking()
            .Where(p => authorIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var projDict = projections.ToDictionary(p => p.Id);

        var responses = items.Select(t =>
        {
            projDict.TryGetValue(t.AuthorId, out var proj);
            var hotScore = HotRankingEngine.CalculateHotScore(t.CreatedAt, t.VoteScore, 0);
            return new ThreadSummaryDto(
                t.Id.Value, t.CommunityId.Value, t.AuthorId.Value,
                proj?.EffectiveName, proj?.AvatarUrl, t.Title, t.Flair,
                t.CreatedAt, hotScore, t.VoteScore);
        }).ToList();

        return new ThreadListDto(responses, total);
    }

    public async Task<ThreadDto?> GetDetailAsync(ThreadDetailCommand request, CancellationToken cancellationToken = default)
    {
        var thread = await _db.Threads.AsNoTracking().FirstOrDefaultAsync(t => t.Id == new ThreadId(request.ThreadId), cancellationToken);
        if (thread is null) return null;

        var proj = await _db.UserProjections.AsNoTracking().FirstOrDefaultAsync(p => p.Id == thread.AuthorId, cancellationToken);
        var hotScore = HotRankingEngine.CalculateHotScore(thread.CreatedAt, thread.VoteScore, 0);
        return new ThreadDto(
            thread.Id.Value, thread.CommunityId.Value, thread.AuthorId.Value,
            proj?.EffectiveName, proj?.AvatarUrl, thread.Title, thread.Content, thread.Flair,
            thread.CreatedAt, thread.EditedAt, thread.IsLocked, thread.IsPinned,
            thread.DeletedAt, hotScore, thread.VoteScore);
    }

    public async Task<FeedListDto> ListFeedAsync(FeedCommand request, CancellationToken cancellationToken = default)
    {
        var baseQuery = _db.Threads.AsNoTracking().Where(t => t.DeletedAt == null);
        var total = await baseQuery.CountAsync(cancellationToken);

        var candidates = await baseQuery
            .OrderByDescending(t => t.CreatedAt)
            .Take(Math.Max(500, request.Page * request.PageSize * 4))
            .ToListAsync(cancellationToken);

        // Load comment counts for all candidates — needed for correct hot scoring
        var candidateIds = candidates.Select(t => t.Id).ToList();
        var commentCounts = await _db.Comments
            .Where(c => candidateIds.Contains(c.ThreadId) && c.DeletedAt == null)
            .GroupBy(c => c.ThreadId)
            .Select(g => new { ThreadId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var commentCountMap = commentCounts.ToDictionary(x => x.ThreadId, x => x.Count);

        List<ForumThread> items;
        if (request.Sort == "hot")
        {
            items = candidates
                .OrderByDescending(t =>
                {
                    commentCountMap.TryGetValue(t.Id, out var cc);
                    return HotRankingEngine.CalculateHotScore(t.CreatedAt, t.VoteScore, cc);
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

        var communityIds = items.Select(t => t.CommunityId).Distinct().ToList();
        var communities = await _db.Communities
            .AsNoTracking()
            .Where(c => communityIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        var communityMap = communities.ToDictionary(c => c.Id);

        var authorIds = items.Select(t => t.AuthorId).Distinct().ToList();
        var projections = await _db.UserProjections
            .AsNoTracking()
            .Where(p => authorIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var projDict = projections.ToDictionary(p => p.Id);

        var responses = items.Select(t =>
        {
            projDict.TryGetValue(t.AuthorId, out var proj);
            communityMap.TryGetValue(t.CommunityId, out var community);
            commentCountMap.TryGetValue(t.Id, out var commentCount);
            var hotScore = HotRankingEngine.CalculateHotScore(t.CreatedAt, t.VoteScore, commentCount);
            return new FeedThreadSummaryDto(
                t.Id.Value, t.CommunityId.Value,
                community?.Slug, community?.Name,
                t.AuthorId.Value,
                proj?.EffectiveName, proj?.AvatarUrl, t.Title, t.Flair,
                t.CreatedAt, hotScore, t.VoteScore, commentCount, t.IsPinned);
        }).ToList();

        return new FeedListDto(responses, total);
    }

    public async Task<ThreadListDto> ListByAuthorAsync(Guid authorId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var authorUserId = new UserId(authorId);
        var query = _db.Threads.AsNoTracking().Where(t => t.AuthorId == authorUserId && t.DeletedAt == null);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var proj = await _db.UserProjections.AsNoTracking().FirstOrDefaultAsync(p => p.Id == authorUserId, cancellationToken);

        var responses = items.Select(t =>
        {
            var hotScore = HotRankingEngine.CalculateHotScore(t.CreatedAt, t.VoteScore, 0);
            return new ThreadSummaryDto(
                t.Id.Value, t.CommunityId.Value, t.AuthorId.Value,
                proj?.EffectiveName, proj?.AvatarUrl, t.Title, t.Flair,
                t.CreatedAt, hotScore, t.VoteScore);
        }).ToList();
        return new ThreadListDto(responses, total);
    }

    public async Task<SearchDto> SearchAsync(SearchQueryCommand request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return new SearchDto(Array.Empty<SearchResultItem>(), 0);

        var results = new List<SearchResultItem>();

        if (request.Scope is SearchScope.All or SearchScope.Communities)
        {
            var communities = await _db.Communities
                .Where(c => EF.Functions.ILike(c.Name, $"%{request.Query}%"))
                .OrderBy(c => c.Name)
                .Take(10)
                .ToListAsync(cancellationToken);

            results.AddRange(communities.Select(c => new SearchResultItem(
                "community", c.Id.Value, c.Name, c.Description, c.Id.Value, c.Slug, c.Name, c.Slug, c.CreatedAt, 0)));
        }

        if (request.Scope is SearchScope.All or SearchScope.Threads)
        {
            var threads = await _db.Threads
                .Where(t => t.DeletedAt == null &&
                    (EF.Functions.ILike(t.Title, $"%{request.Query}%") ||
                     (t.Content != null && EF.Functions.ILike(t.Content, $"%{request.Query}%"))))
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken);

            results.AddRange(threads.Select(t => new SearchResultItem(
                "thread",
                t.Id.Value,
                t.Title,
                t.Content != null && t.Content.Length > 120 ? t.Content[..120] + "\u2026" : t.Content,
                t.CommunityId.Value,
                null,
                null,
                null,
                t.CreatedAt,
                HotRankingEngine.CalculateHotScore(t.CreatedAt, 0, 0))));

            // Backfill community slugs
            var communityIds = threads.Select(t => t.CommunityId).Distinct().ToList();
            var communityMap = await _db.Communities
                .Where(c => communityIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => new { c.Slug, c.Name }, cancellationToken);

            results = results
                .Select(r => r.ItemType == "thread" && communityMap.ContainsKey(new CommunityId(r.CommunityId))
                    ? r with
                    {
                        CommunitySlug = communityMap[new CommunityId(r.CommunityId)].Slug,
                        CommunityName = communityMap[new CommunityId(r.CommunityId)].Name
                    }
                    : r)
                .ToList();
        }

        var ordered = request.Sort == SearchSort.Newest
            ? results.OrderByDescending(r => r.CreatedAt).ToList()
            : results.OrderByDescending(r => r.RankScore).ToList();

        var page = ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new SearchDto(page, ordered.Count);
    }
}
