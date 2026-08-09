using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Queries;
using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Queries;

internal sealed class ModerationQuery : IModerationQuery
{
    private readonly ForumDbContext _db;

    public ModerationQuery(ForumDbContext db) => _db = db;

    public async Task<ModerationQueueDto> QueueAsync(ModerationQueueCommand request, CancellationToken cancellationToken = default)
    {
        var communityId = new CommunityId(request.CommunityId);
        return await FetchQueueAsync(communityId, request.Page, request.PageSize, cancellationToken);
    }

    public async Task<ModerationQueueDto> QueueBySlugAsync(string communitySlug, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var community = await _db.Communities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == communitySlug, cancellationToken);

        if (community is null)
            return new ModerationQueueDto(Array.Empty<ModerationQueueItemDto>(), 0);

        return await FetchQueueAsync(community.Id, page, pageSize, cancellationToken);
    }

    private async Task<ModerationQueueDto> FetchQueueAsync(
        CommunityId communityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Reports
            .AsNoTracking()
            .Where(r => r.CommunityId == communityId && r.Status == ReportStatus.Open);

        // A row is one piece of content, so paging AND the total count are over targets,
        // not reports.
        var total = await query
            .Select(r => new { r.TargetType, r.TargetId })
            .Distinct()
            .CountAsync(cancellationToken);

        var pageTargets = await query
            .GroupBy(r => new { r.TargetType, r.TargetId })
            .Select(g => new { g.Key.TargetType, g.Key.TargetId, Latest = g.Max(r => r.ReportedAt) })
            .OrderByDescending(g => g.Latest)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (pageTargets.Count == 0)
            return new ModerationQueueDto(Array.Empty<ModerationQueueItemDto>(), total);

        // TargetId alone is a safe filter: it is a Guid from either table, so it cannot
        // collide across the two types. The grouping below still keys on the pair.
        var pageTargetIds = pageTargets.Select(t => t.TargetId).ToList();
        var reports = await query
            .Where(r => pageTargetIds.Contains(r.TargetId))
            .ToListAsync(cancellationToken);

        var groups = reports
            .GroupBy(r => (r.TargetType, r.TargetId))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.ReportedAt).ToList());

        // Collect IDs for thread/comment lookups
        var threadTargetIds = groups.Keys
            .Where(k => k.TargetType == ReportTargetType.Thread)
            .Select(k => new ThreadId(k.TargetId))
            .ToList();

        var commentTargetIds = groups.Keys
            .Where(k => k.TargetType == ReportTargetType.Comment)
            .Select(k => new CommentId(k.TargetId))
            .ToList();

        // Load thread data — two-pass: list then stitch
        var threadSummaries = threadTargetIds.Count > 0
            ? await _db.Threads
                .AsNoTracking()
                .Where(t => threadTargetIds.Contains(t.Id))
                .Select(t => new ThreadSummary(t.Id.Value, t.Title, t.AuthorId.Value))
                .ToListAsync(cancellationToken)
            : new List<ThreadSummary>();

        // Load comment data — capture ThreadId too so the queue UI can
        // deep-link a Comment-type report back to its surrounding thread.
        var commentSummaries = commentTargetIds.Count > 0
            ? await _db.Comments
                .AsNoTracking()
                .Where(c => commentTargetIds.Contains(c.Id))
                .Select(c => new CommentSummary(c.Id.Value, c.Content, c.AuthorId.Value, c.ThreadId.Value))
                .ToListAsync(cancellationToken)
            : new List<CommentSummary>();

        // Collect all user IDs (reporters + target authors) for name resolution
        var reporterIds = reports.Select(r => r.ReporterId).Distinct().ToList();
        var targetAuthorUserIds = threadSummaries.Select(t => new UserId(t.AuthorId))
            .Concat(commentSummaries.Select(c => new UserId(c.AuthorId)))
            .Distinct()
            .ToList();
        var allUserIds = reporterIds.Concat(targetAuthorUserIds).Distinct().ToList();

        var userProjections = await _db.UserProjections
            .AsNoTracking()
            .Where(p => allUserIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var userDict = userProjections.ToDictionary(p => p.Id);

        var threadDict = threadSummaries.ToDictionary(t => t.TargetId);
        var commentDict = commentSummaries.ToDictionary(c => c.TargetId);

        var items = pageTargets.Select(t =>
        {
            // Newest first inside the group: the representative report is the
            // most recent one, which is also what the card's timestamp shows.
            var groupReports = groups[(t.TargetType, t.TargetId)];
            var r = groupReports[0];

            string? targetTitle = null;
            Guid? targetAuthorId = null;
            string? targetAuthorName = null;
            // A comment report deep-links to its PARENT thread, so the moderator lands in context.
            Guid? targetThreadId = null;

            if (r.TargetType == ReportTargetType.Thread && threadDict.TryGetValue(r.TargetId, out var thread))
            {
                targetTitle = thread.Title;
                targetAuthorId = thread.AuthorId;
                targetThreadId = thread.TargetId;
                if (userDict.TryGetValue(new UserId(thread.AuthorId), out var authorProj))
                    targetAuthorName = authorProj.EffectiveName;
            }
            else if (r.TargetType == ReportTargetType.Comment && commentDict.TryGetValue(r.TargetId, out var comment))
            {
                var snippet = comment.Content ?? string.Empty;
                targetTitle = snippet.Length > 160 ? snippet[..160] : snippet;
                targetAuthorId = comment.AuthorId;
                targetThreadId = comment.ThreadId;
                if (userDict.TryGetValue(new UserId(comment.AuthorId), out var authorProj))
                    targetAuthorName = authorProj.EffectiveName;
            }

            string? reporterName = null;
            if (userDict.TryGetValue(r.ReporterId, out var reporterProj))
                reporterName = reporterProj.EffectiveName;

            // "reported 3× as advertising" — one reason for the whole group,
            // so it's the one most people picked. Ties break on the newest.
            var reason = groupReports
                .GroupBy(x => x.Reason)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Max(x => x.ReportedAt))
                .First().Key;

            return new ModerationQueueItemDto(
                r.Id.Value,
                r.CommunityId.Value,
                r.TargetType,
                r.TargetId,
                targetThreadId,
                targetTitle,
                targetAuthorId,
                targetAuthorName,
                r.ReporterId.Value,
                reporterName,
                reason,
                r.Details,
                r.ReportedAt,
                groupReports.Count);
        }).ToList();

        return new ModerationQueueDto(items, total);
    }

    public async Task<ModerationLogListDto> ListLogAsync(string communitySlug, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var community = await _db.Communities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == communitySlug, cancellationToken);

        if (community is null)
            return new ModerationLogListDto(Array.Empty<ModerationLogEntryDto>(), 0);

        var query = _db.ModerationLogs
            .AsNoTracking()
            .Where(l => l.CommunityId == community.Id);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(l => l.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Keyed by the VALUE OBJECT, not its Guid — the column has a value converter, so
        // EF only translates `Contains` when both sides are the same type.
        var userIds = rows.Select(l => l.PerformedBy)
            .Concat(rows.Where(l => l.TargetUserId != null).Select(l => l.TargetUserId!))
            .Distinct()
            .ToList();
        var names = await _db.UserProjections.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.EffectiveName, cancellationToken);

        var entries = rows.Select(l =>
        {
            names.TryGetValue(l.PerformedBy, out var by);
            string? target = null;
            if (l.TargetUserId != null) names.TryGetValue(l.TargetUserId, out target);
            return new ModerationLogEntryDto(
                l.Id.Value,
                l.CommunityId.Value,
                l.Action,
                l.PerformedBy.Value,
                l.TargetUserId == null ? (Guid?)null : l.TargetUserId.Value,
                l.TargetContent,
                l.PerformedAt,
                by,
                target);
        }).ToList();

        return new ModerationLogListDto(entries, total);
    }

    public async Task<(Guid CommunityId, Guid ThreadId)?> GetThreadCommunityIdAsync(Guid threadId, CancellationToken cancellationToken = default)
    {
        var tid = new ThreadId(threadId);
        var thread = await _db.Threads
            .AsNoTracking()
            .Where(t => t.Id == tid && t.DeletedAt == null)
            .Select(t => new { CommunityId = t.CommunityId.Value, ThreadId = t.Id.Value })
            .FirstOrDefaultAsync(cancellationToken);

        return thread is null ? null : (thread.CommunityId, thread.ThreadId);
    }

    public async Task<Guid?> GetCommentCommunityIdAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        var cid = new CommentId(commentId);
        var comment = await _db.Comments
            .AsNoTracking()
            .Where(c => c.Id == cid && c.DeletedAt == null)
            .Select(c => new { ThreadId = c.ThreadId })
            .FirstOrDefaultAsync(cancellationToken);

        if (comment is null)
            return null;

        var thread = await _db.Threads
            .AsNoTracking()
            .Where(t => t.Id == comment.ThreadId && t.DeletedAt == null)
            .Select(t => new { CommunityId = t.CommunityId.Value })
            .FirstOrDefaultAsync(cancellationToken);

        return thread?.CommunityId;
    }

    private sealed record ThreadSummary(Guid TargetId, string Title, Guid AuthorId);
    private sealed record CommentSummary(Guid TargetId, string? Content, Guid AuthorId, Guid ThreadId);
}
