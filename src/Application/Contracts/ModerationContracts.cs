using Forum.Domain.ValueObjects;

namespace Forum.Application.Contracts;

public sealed record BanUserRequest(Guid CommunityId, Guid UserId, Guid PerformedByUserId, string? Reason);
public sealed record UnbanUserRequest(Guid BanId, Guid PerformedByUserId);
public sealed record LogModerationActionRequest(
    Guid CommunityId,
    ModerationAction Action,
    Guid PerformedByUserId,
    Guid? TargetUserId,
    string? TargetContent);
public sealed record ModerationQueueRequest(Guid CommunityId, int Page = 1, int PageSize = 20);

public sealed record BanResponse(
    Guid BanId,
    Guid CommunityId,
    Guid UserId,
    DateTime BannedAt,
    string? Reason,
    DateTime? UnbannedAt);

public sealed record ModerationLogEntryResponse(
    Guid LogId,
    Guid CommunityId,
    ModerationAction Action,
    Guid PerformedByUserId,
    Guid? TargetUserId,
    string? TargetContent,
    DateTime PerformedAt);

public sealed record ModerationQueueItemResponse(
    Guid QueueItemId,
    Guid CommunityId,
    string QueueType,
    string Description,
    DateTime QueuedAt);

public sealed record ModerationQueueResponse(IReadOnlyCollection<ModerationQueueItemResponse> Items, int TotalCount);
