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

    public ModerationManager(IBanRepository banRepository, IModerationLogRepository moderationLogRepository)
    {
        _banRepository = banRepository;
        _moderationLogRepository = moderationLogRepository;
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
}
