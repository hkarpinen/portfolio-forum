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
    DateTime PerformedAt,
    /// <summary>Null when the user projection hasn't caught up.</summary>
    string? PerformedByName,
    string? TargetUserName);

/// <summary>One piece of reported CONTENT, not one report — rows group by target.</summary>
public sealed record ModerationQueueItemDto(
    /// <summary>The newest of the group. Resolving it closes its siblings too.</summary>
    Guid QueueItemId,
    Guid CommunityId,
    ReportTargetType TargetType,
    Guid TargetId,
    /// <summary>Equals <see cref="TargetId"/> for a thread; the PARENT thread for a comment.</summary>
    Guid? TargetThreadId,
    string? TargetTitle,
    Guid? TargetAuthorId,
    string? TargetAuthorName,
    Guid ReporterId,
    string? ReporterName,
    /// <summary>The most-cited reason in the group, not the only one.</summary>
    string Reason,
    string? Details,
    /// <summary>The MOST RECENT report in the group, not the first.</summary>
    DateTime ReportedAt,
    /// <summary>Always at least 1.</summary>
    int ReportCount);

public sealed record ModerationQueueDto(IReadOnlyCollection<ModerationQueueItemDto> Items, int TotalCount);

public sealed record ModerationLogListDto(IReadOnlyCollection<ModerationLogEntryDto> Items, int TotalCount);
