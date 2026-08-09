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
        // `Status == Published` excludes drafts from public community listings.
        var query = _db.Threads.AsNoTracking().Where(t =>
            t.CommunityId == communityId
            && t.DeletedAt == null
            && t.Status == ThreadStatus.Published);
        var total = await query.CountAsync(cancellationToken);

        // "hot" needs reply counts before it can order, so it ranks a bounded candidate
        // window in memory. "new" and "top" order in SQL.
        List<ForumThread> items;
        Dictionary<ThreadId, int> commentCountMap;

        if (request.Sort == "hot")
        {
            var candidates = await query
                .OrderByDescending(t => t.CreatedAt)
                .Take(Math.Max(500, request.Page * request.PageSize * 4))
                .ToListAsync(cancellationToken);
            commentCountMap = await CountCommentsAsync(
                candidates.Select(t => t.Id).ToList(), cancellationToken);
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
            var ordered = request.Sort == "top"
                ? query.OrderByDescending(t => t.VoteScore).ThenByDescending(t => t.CreatedAt)
                : query.OrderByDescending(t => t.CreatedAt);
            items = await ordered
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
            commentCountMap = await CountCommentsAsync(
                items.Select(t => t.Id).ToList(), cancellationToken);
        }

        var authorIds = items.Select(t => t.AuthorId).Distinct().ToList();
        var projections = await _db.UserProjections
            .AsNoTracking()
            .Where(p => authorIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var projDict = projections.ToDictionary(p => p.Id);

        var responses = items.Select(t =>
        {
            projDict.TryGetValue(t.AuthorId, out var proj);
            commentCountMap.TryGetValue(t.Id, out var commentCount);
            var hotScore = HotRankingEngine.CalculateHotScore(t.CreatedAt, t.VoteScore, commentCount);
            return new ThreadSummaryDto(
                t.Id.Value, t.CommunityId.Value, t.AuthorId.Value,
                proj?.EffectiveName, proj?.AvatarUrl, t.Title, t.Tags,
                t.CreatedAt, hotScore, t.VoteScore, commentCount, Excerpt(t.Content));
        }).ToList();

        return new ThreadListDto(responses, total);
    }

    /// <summary>Cut on a word boundary, and ellipsised only if something was dropped.</summary>
    private static string? Excerpt(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var flat = string.Join(' ', content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (flat.Length <= 160) return flat;
        var cut = flat[..160];
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > 100) cut = cut[..lastSpace];
        return cut.TrimEnd(',', '.', ';', ':') + "…";
    }

    /// <summary>Excludes deleted comments.</summary>
    private async Task<Dictionary<ThreadId, int>> CountCommentsAsync(
        List<ThreadId> threadIds, CancellationToken cancellationToken)
    {
        if (threadIds.Count == 0) return new Dictionary<ThreadId, int>();
        var counts = await _db.Comments
            .Where(c => threadIds.Contains(c.ThreadId) && c.DeletedAt == null)
            .GroupBy(c => c.ThreadId)
            .Select(g => new { ThreadId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return counts.ToDictionary(x => x.ThreadId, x => x.Count);
    }

    public async Task<ThreadDto?> GetDetailAsync(ThreadDetailCommand request, CancellationToken cancellationToken = default)
    {
        // Public detail read — drafts are scoped to the author and don't
        // surface here. Author-only draft reads go through `GetDraftByIdAsync`.
        var thread = await _db.Threads.AsNoTracking().FirstOrDefaultAsync(
            t => t.Id == new ThreadId(request.ThreadId) && t.Status == ThreadStatus.Published,
            cancellationToken);
        if (thread is null) return null;

        var proj = await _db.UserProjections.AsNoTracking().FirstOrDefaultAsync(p => p.Id == thread.AuthorId, cancellationToken);

        var commentCount = await _db.Comments
            .CountAsync(c => c.ThreadId == thread.Id && c.DeletedAt == null, cancellationToken);
        var hotScore = HotRankingEngine.CalculateHotScore(thread.CreatedAt, thread.VoteScore, commentCount);

        MyVoteDto? myVote = null;
        if (request.CallerId is { } callerId)
        {
            var voterId = new UserId(callerId);
            var vote = await _db.Votes.AsNoTracking().FirstOrDefaultAsync(
                v => v.UserId == voterId
                     && v.TargetType == VoteTargetType.Thread
                     && v.TargetId == thread.Id.Value,
                cancellationToken);
            if (vote is not null) myVote = new MyVoteDto(vote.Id.Value, (int)vote.Direction);
        }

        return new ThreadDto(
            thread.Id.Value, thread.CommunityId.Value, thread.AuthorId.Value,
            proj?.EffectiveName, proj?.AvatarUrl, thread.Title, thread.Content, thread.Tags,
            thread.CreatedAt, thread.EditedAt, thread.IsLocked, thread.IsPinned,
            thread.DeletedAt, hotScore, thread.VoteScore, myVote);
    }

    public async Task<FeedListDto> ListFeedAsync(FeedCommand request, CancellationToken cancellationToken = default)
    {
        // Feed is the home page — drafts never appear here.
        var baseQuery = _db.Threads.AsNoTracking()
            .Where(t => t.DeletedAt == null && t.Status == ThreadStatus.Published);
        var total = await baseQuery.CountAsync(cancellationToken);

        var candidates = await baseQuery
            .OrderByDescending(t => t.CreatedAt)
            .Take(Math.Max(500, request.Page * request.PageSize * 4))
            .ToListAsync(cancellationToken);

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
                proj?.EffectiveName, proj?.AvatarUrl, t.Title, t.Tags,
                t.CreatedAt, hotScore, t.VoteScore, commentCount, t.IsPinned, Excerpt(t.Content));
        }).ToList();

        return new FeedListDto(responses, total);
    }

    public async Task<ThreadListDto> ListByAuthorAsync(Guid authorId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var authorUserId = new UserId(authorId);
        // Public profile listing — anyone can view, so drafts are hidden.
        // The author's own draft list is served by `ListDraftsByAuthorAsync`.
        var query = _db.Threads.AsNoTracking().Where(t =>
            t.AuthorId == authorUserId
            && t.DeletedAt == null
            && t.Status == ThreadStatus.Published);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var proj = await _db.UserProjections.AsNoTracking().FirstOrDefaultAsync(p => p.Id == authorUserId, cancellationToken);

        var authorCommentCounts = await CountCommentsAsync(
            items.Select(t => t.Id).ToList(), cancellationToken);

        var responses = items.Select(t =>
        {
            authorCommentCounts.TryGetValue(t.Id, out var commentCount);
            var hotScore = HotRankingEngine.CalculateHotScore(t.CreatedAt, t.VoteScore, commentCount);
            return new ThreadSummaryDto(
                t.Id.Value, t.CommunityId.Value, t.AuthorId.Value,
                proj?.EffectiveName, proj?.AvatarUrl, t.Title, t.Tags,
                t.CreatedAt, hotScore, t.VoteScore, commentCount, Excerpt(t.Content));
        }).ToList();
        return new ThreadListDto(responses, total);
    }

    public async Task<SearchDto> SearchAsync(SearchQueryCommand request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return new SearchDto(Array.Empty<SearchResultDto>(), 0);

        var results = new List<SearchResultDto>();

        if (request.Scope is SearchScope.All or SearchScope.Communities)
        {
            var communities = await _db.Communities
                .Where(c => EF.Functions.ILike(c.Name, $"%{request.Query}%"))
                .OrderBy(c => c.Name)
                .Take(10)
                .ToListAsync(cancellationToken);

            results.AddRange(communities.Select(c => (SearchResultDto)new CommunitySearchResultDto(
                ItemId: c.Id.Value,
                Name: c.Name,
                Description: c.Description,
                Slug: c.Slug,
                CreatedAt: c.CreatedAt,
                RankScore: 0)));
        }

        if (request.Scope is SearchScope.All or SearchScope.Threads)
        {
            var threads = await _db.Threads
                .Where(t => t.DeletedAt == null
                    && t.Status == ThreadStatus.Published
                    && (EF.Functions.ILike(t.Title, $"%{request.Query}%") ||
                        (t.Content != null && EF.Functions.ILike(t.Content, $"%{request.Query}%"))))
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken);

            results.AddRange(threads.Select(t => (SearchResultDto)new ThreadSearchResultDto(
                ItemId: t.Id.Value,
                Title: t.Title,
                Snippet: t.Content != null && t.Content.Length > 120 ? t.Content[..120] + "\u2026" : t.Content,
                CommunityId: t.CommunityId.Value,
                CommunitySlug: null,
                CommunityName: null,
                CreatedAt: t.CreatedAt,
                RankScore: HotRankingEngine.CalculateHotScore(t.CreatedAt, 0, 0))));

            var communityIds = threads.Select(t => t.CommunityId).Distinct().ToList();
            var communityMap = await _db.Communities
                .Where(c => communityIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => new { c.Slug, c.Name }, cancellationToken);

            results = results
                .Select<SearchResultDto, SearchResultDto>(r =>
                {
                    if (r is ThreadSearchResultDto thread
                        && communityMap.TryGetValue(new CommunityId(thread.CommunityId), out var community))
                    {
                        return thread with { CommunitySlug = community.Slug, CommunityName = community.Name };
                    }
                    return r;
                })
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

    // Drafts are private: every method here filters on author, Draft status and
    // not-deleted, so no path can surface another user's.

    public async Task<IReadOnlyList<ThreadSummaryDto>> ListDraftsByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default)
    {
        var authorUserId = new UserId(authorId);
        var drafts = await _db.Threads.AsNoTracking()
            .Where(t => t.AuthorId == authorUserId
                && t.Status == ThreadStatus.Draft
                && t.DeletedAt == null)
            .OrderByDescending(t => t.SavedAt)
            .ToListAsync(cancellationToken);

        var proj = await _db.UserProjections.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == authorUserId, cancellationToken);

        return drafts.Select(t => new ThreadSummaryDto(
            t.Id.Value, t.CommunityId.Value, t.AuthorId.Value,
            proj?.EffectiveName, proj?.AvatarUrl, t.Title, t.Tags,
            // HotScore is meaningless before publication, so it surfaces 0 rather than a
            // stale rank. A draft has no replies by construction.
            t.CreatedAt, 0d, t.VoteScore, 0, Excerpt(t.Content))).ToList();
    }

    public async Task<ThreadDto?> GetDraftByIdAsync(Guid authorId, Guid threadId, CancellationToken cancellationToken = default)
    {
        var authorUserId = new UserId(authorId);
        var draft = await _db.Threads.AsNoTracking().FirstOrDefaultAsync(t =>
            t.Id == new ThreadId(threadId)
            && t.AuthorId == authorUserId
            && t.Status == ThreadStatus.Draft
            && t.DeletedAt == null,
            cancellationToken);
        if (draft is null) return null;

        var proj = await _db.UserProjections.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == authorUserId, cancellationToken);

        return new ThreadDto(
            draft.Id.Value, draft.CommunityId.Value, draft.AuthorId.Value,
            proj?.EffectiveName, proj?.AvatarUrl, draft.Title, draft.Content, draft.Tags,
            draft.CreatedAt, draft.EditedAt, draft.IsLocked, draft.IsPinned,
            // A draft is unpublished and unvotable, so there is no caller vote.
            draft.DeletedAt, 0d, draft.VoteScore, null);
    }

    public async Task<int> CountDraftsByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default)
    {
        var authorUserId = new UserId(authorId);
        return await _db.Threads.AsNoTracking()
            .CountAsync(t => t.AuthorId == authorUserId
                && t.Status == ThreadStatus.Draft
                && t.DeletedAt == null,
                cancellationToken);
    }
}
