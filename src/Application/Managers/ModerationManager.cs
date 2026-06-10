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

    public ModerationManager(
        IBanRepository banRepository,
        IModerationLogRepository moderationLogRepository,
        IReportRepository reportRepository,
        IMembershipRepository membershipRepository)
    {
        _banRepository = banRepository;
        _moderationLogRepository = moderationLogRepository;
        _reportRepository = reportRepository;
        _membershipRepository = membershipRepository;
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
        var report = await _reportRepository.GetByIdAsync(new ReportId(command.ReportId), cancellationToken)
            ?? throw new InvalidOperationException($"Report {command.ReportId} not found.");

        var moderatorId = new UserId(command.ModeratorId);
        await EnsureCommunityModeratorAsync(report.CommunityId, moderatorId, cancellationToken);
        report.Approve(moderatorId);

        var log = ModerationLog.Create(
            report.CommunityId,
            ModerationAction.ResolveReportApproved,
            moderatorId,
            null,
            $"Approved report {report.Id.Value} ({report.TargetType}: {report.TargetId})");

        await _moderationLogRepository.AddAsync(log, cancellationToken);
        await _reportRepository.CommitAsync(cancellationToken);
    }

    public async Task RemoveContentAsync(RemoveContentCommand command, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(new ReportId(command.ReportId), cancellationToken)
            ?? throw new InvalidOperationException($"Report {command.ReportId} not found.");

        var moderatorId = new UserId(command.ModeratorId);
        await EnsureCommunityModeratorAsync(report.CommunityId, moderatorId, cancellationToken);
        report.RemoveContent(moderatorId);

        var log = ModerationLog.Create(
            report.CommunityId,
            ModerationAction.ResolveReportRemoved,
            moderatorId,
            null,
            $"Removed content for report {report.Id.Value} ({report.TargetType}: {report.TargetId})");

        await _moderationLogRepository.AddAsync(log, cancellationToken);
        await _reportRepository.CommitAsync(cancellationToken);
    }

    public async Task DismissReportAsync(DismissReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(new ReportId(command.ReportId), cancellationToken)
            ?? throw new InvalidOperationException($"Report {command.ReportId} not found.");

        var moderatorId = new UserId(command.ModeratorId);
        await EnsureCommunityModeratorAsync(report.CommunityId, moderatorId, cancellationToken);
        report.Dismiss(moderatorId);

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
    /// Authorizes a report-resolution action against the report's own community.
    /// The caller must hold a moderator or owner role in that specific community —
    /// class-level <c>MemberOrAbove</c> only proves membership in *some* community,
    /// which would otherwise let any member resolve any community's mod queue.
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
