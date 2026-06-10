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
    ReportTargetType TargetType,
    Guid TargetId,
    /// <summary>
    /// The thread to deep-link from the mod queue. For Thread-type reports
    /// this equals <see cref="TargetId"/>; for Comment-type reports it's
    /// the parent thread so the moderator can jump into context.
    /// </summary>
    Guid? TargetThreadId,
    string? TargetTitle,
    Guid? TargetAuthorId,
    string? TargetAuthorName,
    Guid ReporterId,
    string? ReporterName,
    string Reason,
    string? Details,
    DateTime ReportedAt);

public sealed record ModerationQueueDto(IReadOnlyCollection<ModerationQueueItemDto> Items, int TotalCount);

public sealed record ModerationLogListDto(IReadOnlyCollection<ModerationLogEntryDto> Items, int TotalCount);
