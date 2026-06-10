using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record ReportResolved(
    ReportId ReportId,
    CommunityId CommunityId,
    ReportTargetType TargetType,
    Guid TargetId,
    UserId ResolvedByUserId,
    ReportStatus Status,
    DateTime ResolvedAt);
