using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Mappers;
using Forum.Domain.Aggregates;
using Forum.Application.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class ModerationManager : IModerationManager
{
    private readonly IBanRepository _banRepository;
    private readonly IModerationLogRepository _moderationLogRepository;
    private readonly IReportRepository _reportRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IThreadRepository _threadRepository;
    private readonly ICommentRepository _commentRepository;

    public ModerationManager(
        IBanRepository banRepository,
        IModerationLogRepository moderationLogRepository,
        IReportRepository reportRepository,
        IMembershipRepository membershipRepository,
        IThreadRepository threadRepository,
        ICommentRepository commentRepository)
    {
        _banRepository = banRepository;
        _moderationLogRepository = moderationLogRepository;
        _reportRepository = reportRepository;
        _membershipRepository = membershipRepository;
        _threadRepository = threadRepository;
        _commentRepository = commentRepository;
    }

    public async Task<BanDto> BanAsync(BanUserCommand command, CancellationToken cancellationToken = default)
    {
        var ban = CommunityBan.Create(
            new CommunityId(command.CommunityId),
            new UserId(command.UserId),
            command.Reason);

        await _banRepository.AddAsync(ban, cancellationToken);

        var log = ModerationLog.Create(
            new CommunityId(command.CommunityId),
            ModerationAction.BanUser,
            new UserId(command.PerformedByUserId),
            new UserId(command.UserId),
            command.Reason);

        await _moderationLogRepository.AddAsync(log, cancellationToken);
        await _banRepository.CommitAsync(cancellationToken);
        return ModerationMapper.ToDto(ban);
    }

    public async Task<BanDto?> UnbanAsync(UnbanUserCommand command, CancellationToken cancellationToken = default)
    {
        var ban = await _banRepository.GetByIdAsync(new BanId(command.BanId), cancellationToken);

        if (ban is null)
            return null;

        ban.Unban(DateTime.UtcNow);
        await _banRepository.RemoveAsync(ban, cancellationToken);

        var log = ModerationLog.Create(
            ban.CommunityId,
            ModerationAction.UnbanUser,
            new UserId(command.PerformedByUserId),
            ban.UserId,
            null);

        await _moderationLogRepository.AddAsync(log, cancellationToken);
        await _banRepository.CommitAsync(cancellationToken);
        return ModerationMapper.ToDto(ban);
    }

    public async Task<ModerationLogEntryDto> LogAsync(LogModerationActionCommand command, CancellationToken cancellationToken = default)
    {
        var targetUserId = command.TargetUserId.HasValue
            ? new UserId(command.TargetUserId.Value)
            : null;

        var log = ModerationLog.Create(
            new CommunityId(command.CommunityId),
            command.Action,
            new UserId(command.PerformedByUserId),
            targetUserId,
            command.TargetContent);

        await _moderationLogRepository.AddAsync(log, cancellationToken);
        await _moderationLogRepository.CommitAsync(cancellationToken);
        return ModerationMapper.ToDto(log);
    }

    public async Task<Guid> ReportContentAsync(ReportContentCommand command, CancellationToken cancellationToken = default)
    {
        var report = Report.Create(
            new CommunityId(command.CommunityId),
            command.TargetType,
            command.TargetId,
            new UserId(command.ReportedByUserId),
            command.Reason,
            command.Details);

        await _reportRepository.AddAsync(report, cancellationToken);
        await _reportRepository.CommitAsync(cancellationToken);
        return report.Id.Value;
    }

    public async Task ApproveReportAsync(ApproveReportCommand command, CancellationToken cancellationToken = default)
    {
        var (report, siblings, moderatorId) = await OpenGroupAsync(command.ReportId, command.ModeratorId, cancellationToken);

        foreach (var sibling in siblings)
            sibling.Approve(moderatorId);

        var log = ModerationLog.Create(
            report.CommunityId,
            ModerationAction.ResolveReportApproved,
            moderatorId,
            null,
            $"Approved report {report.Id.Value} ({report.TargetType}: {report.TargetId})");

        await _moderationLogRepository.AddAsync(log, cancellationToken);
        await _reportRepository.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Soft-deletes the content — the same delete the author's own performs, so the
    /// existing filters take it off every surface — and closes every open report on it.
    ///
    /// Logged as DeleteThread/DeleteComment, not as a report resolution, so the public
    /// log names the content and reason rather than an internal report id.
    /// </summary>
    public async Task RemoveContentAsync(RemoveContentCommand command, CancellationToken cancellationToken = default)
    {
        var (report, siblings, moderatorId) = await OpenGroupAsync(command.ReportId, command.ModeratorId, cancellationToken);

        UserId? targetAuthorId = null;
        var action = ModerationAction.DeleteThread;

        if (report.TargetType == ReportTargetType.Thread)
        {
            var thread = await _threadRepository.GetByIdAsync(new ThreadId(report.TargetId), cancellationToken);
            if (thread is not null)
            {
                thread.Delete(DateTime.UtcNow);
                await _threadRepository.UpdateAsync(thread, cancellationToken);
                targetAuthorId = thread.AuthorId;
            }
        }
        else if (report.TargetType == ReportTargetType.Comment)
        {
            action = ModerationAction.DeleteComment;
            var comment = await _commentRepository.GetByIdAsync(new CommentId(report.TargetId), cancellationToken);
            if (comment is not null)
            {
                comment.Delete(DateTime.UtcNow);
                await _commentRepository.UpdateAsync(comment, cancellationToken);
                targetAuthorId = comment.AuthorId;
            }
        }

        // The group's DOMINANT reason, which is what the moderator saw. The named
        // report is only the newest, so its reason can disagree with the card.
        var reason = siblings
            .GroupBy(r => r.Reason)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Max(r => r.ReportedAt))
            .First().Key;

        foreach (var sibling in siblings)
            sibling.RemoveContent(moderatorId);

        var log = ModerationLog.Create(
            report.CommunityId,
            action,
            moderatorId,
            targetAuthorId,
            reason);

        await _moderationLogRepository.AddAsync(log, cancellationToken);
        await _reportRepository.CommitAsync(cancellationToken);
    }

    public async Task DismissReportAsync(DismissReportCommand command, CancellationToken cancellationToken = default)
    {
        var (report, siblings, moderatorId) = await OpenGroupAsync(command.ReportId, command.ModeratorId, cancellationToken);

        foreach (var sibling in siblings)
            sibling.Dismiss(moderatorId);

        var log = ModerationLog.Create(
            report.CommunityId,
            ModerationAction.ResolveReportDismissed,
            moderatorId,
            null,
            $"Dismissed report {report.Id.Value} ({report.TargetType}: {report.TargetId})");

        await _moderationLogRepository.AddAsync(log, cancellationToken);
        await _reportRepository.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Returns every still-open report on the same content, the named one included.
    /// One click must close the whole group or the card reappears on the next refetch.
    /// </summary>
    private async Task<(Report Report, IReadOnlyList<Report> Siblings, UserId ModeratorId)> OpenGroupAsync(
        Guid reportId,
        Guid moderatorGuid,
        CancellationToken cancellationToken)
    {
        var report = await _reportRepository.GetByIdAsync(new ReportId(reportId), cancellationToken)
            ?? throw new InvalidOperationException($"Report {reportId} not found.");

        var moderatorId = new UserId(moderatorGuid);
        await EnsureCommunityModeratorAsync(report.CommunityId, moderatorId, cancellationToken);

        var siblings = await _reportRepository.ListOpenByTargetAsync(
            report.CommunityId, report.TargetType, report.TargetId, cancellationToken);

        // It may be absent if a concurrent moderator already closed it — still attempt
        // the transition so the caller gets the "already resolved" error.
        if (!siblings.Any(s => s.Id == report.Id))
            siblings = siblings.Append(report).ToList();

        return (report, siblings, moderatorId);
    }

    /// <summary>
    /// Must moderate THAT community. The class-level policy only proves membership in
    /// some community, which would let any member resolve any community's queue.
    /// </summary>
    private async Task EnsureCommunityModeratorAsync(
        CommunityId communityId,
        UserId moderatorId,
        CancellationToken cancellationToken)
    {
        var membership = await _membershipRepository.GetByUserAndCommunityAsync(
            moderatorId, communityId, cancellationToken);

        if (membership?.Role is not (CommunityRole.Owner or CommunityRole.Moderator))
            throw new UnauthorizedAccessException(
                "Only a moderator or owner of this community can resolve its reports.");
    }
}
