using Forum.Application.Contracts;
using Forum.Application.Queries;
using Forum.Domain.Aggregates;
using Forum.Domain.Engines;
using Forum.Domain.ReadModels;
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

        var threadIds = items.Select(t => t.Id.Value).ToList();
        var scores = await GetScoresByTargetsAsync(VoteTargetType.Thread, threadIds, cancellationToken);

        var authorIds = items.Select(t => t.AuthorId).Distinct().ToList();
        var projections = await _db.UserProjections
            .Where(p => authorIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var projDict = projections.ToDictionary(p => p.Id);

        var responses = items.Select(t =>
        {
            projDict.TryGetValue(t.AuthorId, out var proj);
            return Map(t, scores.GetValueOrDefault(t.Id.Value, 0), 0, proj);
        }).ToList();

        return new ThreadListResponse(responses, total);
    }

    public async Task<ThreadResponse?> GetDetailAsync(ThreadDetailRequest request, CancellationToken cancellationToken = default)
    {
        var thread = await _db.Threads.FirstOrDefaultAsync(t => t.Id == new ThreadId(request.ThreadId), cancellationToken);
        if (thread is null) return null;

        var score = await GetScoreByTargetAsync(VoteTargetType.Thread, thread.Id.Value, cancellationToken);
        var proj = await _db.UserProjections.FirstOrDefaultAsync(p => p.Id == thread.AuthorId, cancellationToken);
        return Map(thread, score, 0, proj);
    }

    private async Task<int> GetScoreByTargetAsync(VoteTargetType targetType, Guid targetId, CancellationToken cancellationToken)
    {
        var directions = await _db.Votes
            .Where(v => v.TargetType == targetType && v.TargetId == targetId && v.RetractedAt == null)
            .Select(v => v.Direction)
            .ToListAsync(cancellationToken);
        return directions.Sum(d => (int)d);
    }

    private async Task<Dictionary<Guid, int>> GetScoresByTargetsAsync(VoteTargetType targetType, IEnumerable<Guid> targetIds, CancellationToken cancellationToken)
    {
        var ids = targetIds.ToList();
        var votes = await _db.Votes
            .Where(v => v.TargetType == targetType && ids.Contains(v.TargetId) && v.RetractedAt == null)
            .Select(v => new { v.TargetId, v.Direction })
            .ToListAsync(cancellationToken);
        return votes.GroupBy(v => v.TargetId).ToDictionary(g => g.Key, g => g.Sum(v => (int)v.Direction));
    }

    private ThreadResponse Map(ForumThread t, int score, int commentCount, UserProjection? proj)
    {
        var hotScore = _hotRankingEngine.CalculateHotScore(t.CreatedAt, score, commentCount);
        return new ThreadResponse(
            t.Id.Value,
            t.CommunityId.Value,
            t.AuthorId.Value,
            proj?.DisplayName ?? proj?.UserName,
            proj?.AvatarUrl,
            t.Title,
            t.Content,
            t.CreatedAt,
            t.EditedAt,
            t.IsLocked,
            t.IsPinned,
            t.DeletedAt,
            hotScore,
            score);
    }
}
