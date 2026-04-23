using Forum.Application.Contracts;
using Forum.Application.Queries;
using Forum.Domain.Engines;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Queries;

internal sealed class SearchQuery : ISearchQuery
{
    private readonly ForumDbContext _db;
    private readonly IHotRankingEngine _hotRankingEngine;

    public SearchQuery(ForumDbContext db, IHotRankingEngine hotRankingEngine)
    {
        _db = db;
        _hotRankingEngine = hotRankingEngine;
    }

    public async Task<SearchResponse> QueryAsync(SearchQueryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return new SearchResponse(Array.Empty<SearchResultItem>(), 0);

        var results = new List<SearchResultItem>();

        if (request.Scope is SearchScope.All or SearchScope.Communities)
        {
            var communities = await _db.Communities
                .Where(c => EF.Functions.ILike(c.Name, $"%{request.Query}%"))
                .OrderBy(c => c.Name)
                .Take(10)
                .ToListAsync(cancellationToken);

            results.AddRange(communities.Select(c => new SearchResultItem(
                "community", c.Id.Value, c.Name, null, c.Id.Value, c.CreatedAt, 0)));
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
                t.Content != null && t.Content.Length > 120 ? t.Content[..120] + "…" : t.Content,
                t.CommunityId.Value,
                t.CreatedAt,
                _hotRankingEngine.CalculateHotScore(t.CreatedAt, 0, 0))));
        }

        var ordered = request.Sort == SearchSort.Newest
            ? results.OrderByDescending(r => r.CreatedAt).ToList()
            : results.OrderByDescending(r => r.RankScore).ToList();

        var page = ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new SearchResponse(page, ordered.Count);
    }
}
