using Forum.Domain.ValueObjects;
using Forum.Domain.Events;

namespace Forum.Domain.Aggregates;

public enum ReportStatus { Open, Approved, Removed, Dismissed }

public sealed class Report : IAggregateRoot
{
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public ReportId Id { get; private set; }
    public CommunityId CommunityId { get; private set; }
    public ReportTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public UserId ReporterId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? Details { get; private set; }
    public DateTime ReportedAt { get; private set; }
    public ReportStatus Status { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public UserId? ResolvedByUserId { get; private set; }

    private Report() { }

    public static Report Create(
        CommunityId communityId,
        ReportTargetType targetType,
        Guid targetId,
        UserId reporterId,
        string reason,
        string? details)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required", nameof(reason));
        if (reason.Length > 200)
            throw new ArgumentException("Reason too long", nameof(reason));
        if (details is { Length: > 2000 })
            throw new ArgumentException("Details too long", nameof(details));

        var now = DateTime.UtcNow;
        var report = new Report
        {
            Id = ReportId.New(),
            CommunityId = communityId,
            TargetType = targetType,
            TargetId = targetId,
            ReporterId = reporterId,
            Reason = reason.Trim(),
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            ReportedAt = now,
            Status = ReportStatus.Open
        };
        return report;
    }

    public void Approve(UserId moderatorId)
    {
        Transition(ReportStatus.Approved, moderatorId);
    }

    public void RemoveContent(UserId moderatorId)
    {
        Transition(ReportStatus.Removed, moderatorId);
    }

    public void Dismiss(UserId moderatorId)
    {
        Transition(ReportStatus.Dismissed, moderatorId);
    }

    private void Transition(ReportStatus newStatus, UserId moderatorId)
    {
        if (Status != ReportStatus.Open)
            throw new InvalidOperationException($"Report is already {Status}");

        Status = newStatus;
        ResolvedAt = DateTime.UtcNow;
        ResolvedByUserId = moderatorId;
        _domainEvents.Add(new ReportResolved(Id, CommunityId, TargetType, TargetId, moderatorId, newStatus, ResolvedAt.Value));
    }
}
