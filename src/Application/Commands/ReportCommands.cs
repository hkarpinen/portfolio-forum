using Forum.Domain.ValueObjects;

namespace Forum.Application.Commands;

public sealed record ReportContentCommand(
    Guid CommunityId,
    ReportTargetType TargetType,
    Guid TargetId,
    string Reason,
    string? Details,
    Guid ReportedByUserId = default);

public sealed record ApproveReportCommand(Guid ReportId, Guid ModeratorId = default);
public sealed record RemoveContentCommand(Guid ReportId, Guid ModeratorId = default);
public sealed record DismissReportCommand(Guid ReportId, Guid ModeratorId = default);
