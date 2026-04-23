using Forum.Application.Contracts;
using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class ModerationManager : IModerationManager
{
    private readonly IBanRepository _banRepository;
    private readonly IModerationLogRepository _moderationLogRepository;

    public ModerationManager(IBanRepository banRepository, IModerationLogRepository moderationLogRepository)
    {
        _banRepository = banRepository;
        _moderationLogRepository = moderationLogRepository;
    }

    public async Task<BanResponse> BanAsync(BanUserRequest request, CancellationToken cancellationToken = default)
    {
        var ban = CommunityBan.Create(
            new CommunityId(request.CommunityId),
            new UserId(request.UserId),
            request.Reason);

        await _banRepository.AddAsync(ban, cancellationToken);

        var log = ModerationLog.Create(
            new CommunityId(request.CommunityId),
            ModerationAction.BanUser,
            new UserId(request.PerformedByUserId),
            new UserId(request.UserId),
            request.Reason);

        await _moderationLogRepository.AddAsync(log, cancellationToken);
        return Map(ban);
    }

    public async Task<BanResponse?> UnbanAsync(UnbanUserRequest request, CancellationToken cancellationToken = default)
    {
        var ban = await _banRepository.GetByIdAsync(new BanId(request.BanId), cancellationToken);

        if (ban is null)
        {
            return null;
        }

        ban.Unban(DateTime.UtcNow);
        await _banRepository.RemoveAsync(ban.Id, cancellationToken);

        var log = ModerationLog.Create(
            ban.CommunityId,
            ModerationAction.UnbanUser,
            new UserId(request.PerformedByUserId),
            ban.UserId,
            null);

        await _moderationLogRepository.AddAsync(log, cancellationToken);
        return Map(ban);
    }

    public async Task<ModerationLogEntryResponse> LogAsync(LogModerationActionRequest request, CancellationToken cancellationToken = default)
    {
        var targetUserId = request.TargetUserId.HasValue
            ? new UserId(request.TargetUserId.Value)
            : null;

        var log = ModerationLog.Create(
            new CommunityId(request.CommunityId),
            request.Action,
            new UserId(request.PerformedByUserId),
            targetUserId,
            request.TargetContent);

        await _moderationLogRepository.AddAsync(log, cancellationToken);
        return Map(log);
    }

    private static BanResponse Map(CommunityBan ban)
        => new(
            ban.Id.Value,
            ban.CommunityId.Value,
            ban.UserId.Value,
            ban.BannedAt,
            ban.Reason,
            ban.UnbannedAt);

    private static ModerationLogEntryResponse Map(ModerationLog log)
        => new(
            log.Id.Value,
            log.CommunityId.Value,
            log.Action,
            log.PerformedBy.Value,
            log.TargetUserId?.Value,
            log.TargetContent,
            log.PerformedAt);
}
