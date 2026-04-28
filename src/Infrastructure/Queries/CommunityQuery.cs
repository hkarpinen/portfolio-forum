using Forum.Application.Contracts;
using Forum.Application.Queries;
using Forum.Domain.Aggregates;
using Forum.Domain.Engines;
using Forum.Domain.ReadModels;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Queries;

internal sealed class CommunityQuery : ICommunityQuery
{
    private readonly ForumDbContext _db;
    private readonly IHotRankingEngine _hotRankingEngine;

    public CommunityQuery(ForumDbContext db, IHotRankingEngine hotRankingEngine)
    {
        _db = db;
        _hotRankingEngine = hotRankingEngine;
    }

    public async Task<CommunityListResponse> ListAsync(ListCommunitiesRequest request, CancellationToken cancellationToken = default)
    {
        var total = await _db.Communities.CountAsync(cancellationToken);
        var communities = await _db.Communities
            .OrderBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var communityIds = communities.Select(c => c.Id).ToList();

        // Load non-deleted threads for the page of communities.
        var allCandidateThreads = await _db.Threads
            .Where(t => communityIds.Contains(t.CommunityId) && t.DeletedAt == null)
            .ToListAsync(cancellationToken);

        // Thread counts per community
        var threadCountByCommunity = allCandidateThreads
            .GroupBy(t => t.CommunityId.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // Comment counts per thread (used in hot score and total)
        var threadIdVOs = allCandidateThreads.Select(t => t.Id).ToList();
        Dictionary<Guid, int> commentCounts;
        if (threadIdVOs.Count == 0)
        {
            commentCounts = new Dictionary<Guid, int>();
        }
        else
        {
            var commentRows = await _db.Comments
                .Where(c => threadIdVOs.Contains(c.ThreadId) && c.DeletedAt == null)
                .GroupBy(c => c.ThreadId)
                .Select(g => new { ThreadId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            commentCounts = commentRows.ToDictionary(x => x.ThreadId.Value, x => x.Count);
        }

        // Total comment count per community
        var commentCountByCommunity = allCandidateThreads
            .GroupBy(t => t.CommunityId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(t => commentCounts.GetValueOrDefault(t.Id.Value, 0)));

        // Member counts per community
        var memberRows = await _db.Memberships
            .Where(m => communityIds.Contains(m.CommunityId))
            .GroupBy(m => m.CommunityId)
            .Select(g => new { CommunityId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var memberCountByCommunity = memberRows.ToDictionary(x => x.CommunityId.Value, x => x.Count);

        // Pick the hottest thread per community (score already on the row — no votes join)
        var hottestByCommunity = allCandidateThreads
            .GroupBy(t => t.CommunityId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(t => _hotRankingEngine.CalculateHotScore(
                    t.CreatedAt,
                    t.VoteScore,
                    commentCounts.GetValueOrDefault(t.Id.Value, 0)
                )).First());

        // Fetch user projections for thread authors + latest reply authors
        var authorUserIds = hottestByCommunity.Values.Select(t => t.AuthorId).Distinct().ToList();

        // Latest (non-deleted) reply per hottest thread
        var hottestThreadVOs = hottestByCommunity.Values.Select(t => t.Id).ToList();
        var latestReplyByThreadId = hottestThreadVOs.Count == 0
            ? new Dictionary<Guid, Comment>()
            : (await _db.Comments
                .Where(c => hottestThreadVOs.Contains(c.ThreadId) && c.DeletedAt == null)
                .GroupBy(c => c.ThreadId)
                .Select(g => g.OrderByDescending(c => c.CreatedAt).First())
                .ToListAsync(cancellationToken))
                .ToDictionary(c => c.ThreadId.Value);

        var replyAuthorUserIds = latestReplyByThreadId.Values.Select(r => r.AuthorId).Distinct().ToList();
        var allUserIds = authorUserIds.Concat(replyAuthorUserIds).Distinct().ToList();

        var projections = allUserIds.Count == 0
            ? new Dictionary<Guid, UserProjection>()
            : (await _db.UserProjections
                .Where(p => allUserIds.Contains(p.Id))
                .ToListAsync(cancellationToken))
                .ToDictionary(p => p.Id.Value);

        // Build responses ordered by best hot-score first
        var responses = communities
            .Select(c =>
            {
                CommunityActivitySnapshot? activity = null;
                if (hottestByCommunity.TryGetValue(c.Id, out var thread))
                {
                    var hotScore = _hotRankingEngine.CalculateHotScore(
                        thread.CreatedAt,
                        thread.VoteScore,
                        commentCounts.GetValueOrDefault(thread.Id.Value, 0));

                    projections.TryGetValue(thread.AuthorId.Value, out var authorProj);

                    DateTime? latestReplyAt = null;
                    string? latestReplyAuthorName = null;
                    string? latestReplyAuthorAvatar = null;

                    if (latestReplyByThreadId.TryGetValue(thread.Id.Value, out var reply))
                    {
                        latestReplyAt = reply.CreatedAt;
                        projections.TryGetValue(reply.AuthorId.Value, out var replyProj);
                        latestReplyAuthorName = replyProj?.EffectiveName;
                        latestReplyAuthorAvatar = replyProj?.AvatarUrl;
                    }

                    activity = new CommunityActivitySnapshot(
                        thread.Id.Value,
                        thread.Title,
                        thread.CreatedAt,
                        hotScore,
                        authorProj?.EffectiveName,
                        authorProj?.AvatarUrl,
                        latestReplyAt,
                        latestReplyAuthorName,
                        latestReplyAuthorAvatar);
                }

                return (Community: c, Activity: activity,
                    MemberCount: memberCountByCommunity.GetValueOrDefault(c.Id.Value, 0),
                    ThreadCount: threadCountByCommunity.GetValueOrDefault(c.Id.Value, 0),
                    CommentCount: commentCountByCommunity.GetValueOrDefault(c.Id.Value, 0));
            })
            .OrderByDescending(x => x.Activity?.HotScore ?? double.MinValue)
            .Select(x => MapResponse(x.Community, x.Activity, x.MemberCount, x.ThreadCount, x.CommentCount))
            .ToList();

        return new CommunityListResponse(responses, total);
    }

    public async Task<CommunityResponse?> GetDetailAsync(CommunityDetailRequest request, CancellationToken cancellationToken = default)
    {
        var community = await _db.Communities.FirstOrDefaultAsync(c => c.Id == new CommunityId(request.CommunityId), cancellationToken);
        return community is null ? null : MapResponse(community);
    }

    public async Task<CommunityResponse?> GetBySlugAsync(CommunityBySlugRequest request, CancellationToken cancellationToken = default)
    {
        var community = await _db.Communities.FirstOrDefaultAsync(c => c.Slug == request.Slug, cancellationToken);
        return community is null ? null : MapResponse(community);
    }

    private static CommunityResponse MapResponse(Community c, CommunityActivitySnapshot? activity = null, int memberCount = 0, int threadCount = 0, int commentCount = 0) => new(
        c.Id.Value,
        c.Slug,
        c.Name,
        c.Description,
        c.ImageUrl,
        c.Visibility,
        c.OwnerId.Value,
        c.CreatedAt,
        c.UpdatedAt,
        activity,
        memberCount,
        threadCount,
        commentCount);
}
