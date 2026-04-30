using Forum.Application.Contracts;
using Forum.Domain.Aggregates;

namespace Forum.Application.Mappers;

public static class ModerationMapper
{
    public static BanResponse ToResponse(CommunityBan ban) => new(
        ban.Id.Value,
        ban.CommunityId.Value,
        ban.UserId.Value,
        ban.BannedAt,
        ban.Reason,
        ban.UnbannedAt);

    public static ModerationLogEntryResponse ToResponse(ModerationLog log) => new(
        log.Id.Value,
        log.CommunityId.Value,
        log.Action,
        log.PerformedBy.Value,
        log.TargetUserId?.Value,
        log.TargetContent,
        log.PerformedAt);
}
