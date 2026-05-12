using Forum.Domain.ValueObjects;

namespace Forum.Application.Dtos;

public sealed record BanDto(
    Guid BanId,
    Guid CommunityId,
    Guid UserId,
    DateTime BannedAt,
    string? Reason,
    DateTime? UnbannedAt);

public sealed record ModerationLogEntryDto(
    Guid LogId,
    Guid CommunityId,
    ModerationAction Action,
    Guid PerformedByUserId,
    Guid? TargetUserId,
    string? TargetContent,
    DateTime PerformedAt);

public sealed record ModerationQueueItemDto(
    Guid QueueItemId,
    Guid CommunityId,
    string QueueType,
    string Description,
    DateTime QueuedAt);

public sealed record ModerationQueueDto(IReadOnlyCollection<ModerationQueueItemDto> Items, int TotalCount);
