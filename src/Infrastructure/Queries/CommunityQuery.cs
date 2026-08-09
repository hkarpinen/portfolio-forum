using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Mappers;
using Forum.Application.Queries;
using Forum.Domain.Aggregates;
using Forum.Domain.Engines;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Projections;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Queries;

internal sealed class CommunityQuery : ICommunityQuery
{
    private readonly ForumDbContext _db;

    public CommunityQuery(ForumDbContext db) => _db = db;

    public async Task<CommunityListDto> ListAsync(ListCommunitiesCommand request, CancellationToken cancellationToken = default)
    {
        // Filters to the caller's own communities when set.
        IQueryable<Forum.Domain.Aggregates.Community> baseQuery = _db.Communities.AsNoTracking();
        if (request.MembershipUserId is { } userId)
        {
            var userIdVo = new UserId(userId);
            baseQuery = baseQuery.Where(c => _db.Memberships.Any(m =>
                m.CommunityId == c.Id && m.UserId == userIdVo));
        }

        var total = await baseQuery.CountAsync(cancellationToken);
        var communities = await baseQuery
            .OrderBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var communityIds = communities.Select(c => c.Id).ToList();

        var allCandidateThreads = await _db.Threads
            .AsNoTracking()
            .Where(t => communityIds.Contains(t.CommunityId) && t.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var threadCountByCommunity = allCandidateThreads
            .GroupBy(t => t.CommunityId.Value)
            .ToDictionary(g => g.Key, g => g.Count());

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

        var commentCountByCommunity = allCandidateThreads
            .GroupBy(t => t.CommunityId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(t => commentCounts.GetValueOrDefault(t.Id.Value, 0)));

        var memberRows = await _db.Memberships
            .Where(m => communityIds.Contains(m.CommunityId))
            .GroupBy(m => m.CommunityId)
            .Select(g => new { CommunityId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var memberCountByCommunity = memberRows.ToDictionary(x => x.CommunityId.Value, x => x.Count);

        // Score is already on the row, so no votes join is needed here.
        var hottestByCommunity = allCandidateThreads
            .GroupBy(t => t.CommunityId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(t => HotRankingEngine.CalculateHotScore(
                    t.CreatedAt,
                    t.VoteScore,
                    commentCounts.GetValueOrDefault(t.Id.Value, 0)
                )).First());

        var authorUserIds = hottestByCommunity.Values.Select(t => t.AuthorId).Distinct().ToList();

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
                .AsNoTracking()
                .Where(p => allUserIds.Contains(p.Id))
                .ToListAsync(cancellationToken))
                .ToDictionary(p => p.Id.Value);

        var responses = communities
            .Select(c =>
            {
                CommunityActivitySnapshot? activity = null;
                if (hottestByCommunity.TryGetValue(c.Id, out var thread))
                {
                    var hotScore = HotRankingEngine.CalculateHotScore(
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
            .Select(x => CommunityMapper.ToDto(x.Community, x.Activity, x.MemberCount, x.ThreadCount, x.CommentCount))
            .ToList();

        return new CommunityListDto(responses, total);
    }

    public async Task<CommunityDto?> GetDetailAsync(CommunityDetailCommand request, CancellationToken cancellationToken = default)
    {
        var community = await _db.Communities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == new CommunityId(request.CommunityId), cancellationToken);
        return community is null ? null : CommunityMapper.ToDto(community);
    }

    public async Task<CommunityDto?> GetBySlugAsync(CommunityBySlugCommand request, CancellationToken cancellationToken = default)
    {
        var community = await _db.Communities.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == request.Slug, cancellationToken);
        return community is null ? null : CommunityMapper.ToDto(community);
    }
}
