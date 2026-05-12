using Forum.Domain.ValueObjects;

namespace Forum.Application.Commands;

public sealed record BanUserCommand(Guid CommunityId, Guid UserId, string? Reason, Guid PerformedByUserId = default);
public sealed record UnbanUserCommand(Guid BanId, Guid PerformedByUserId = default);
public sealed record LogModerationActionCommand(Guid CommunityId, ModerationAction Action, Guid? TargetUserId, string? TargetContent, Guid PerformedByUserId = default);
public sealed record ModerationQueueCommand(Guid CommunityId, int Page = 1, int PageSize = 20);
