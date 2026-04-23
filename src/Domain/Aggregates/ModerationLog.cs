using Forum.Domain.ValueObjects;
using Forum.Domain.Events;

namespace Forum.Domain.Aggregates;

public sealed class ModerationLog
{
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public LogId Id { get; private set; }
    public CommunityId CommunityId { get; private set; }
    public ModerationAction Action { get; private set; }
    public UserId PerformedBy { get; private set; }
    public UserId? TargetUserId { get; private set; }
    public string? TargetContent { get; private set; }
    public DateTime PerformedAt { get; private set; }

    private ModerationLog() { }

    public static ModerationLog Create(
        CommunityId communityId,
        ModerationAction action,
        UserId performedBy,
        UserId? targetUserId,
        string? targetContent)
    {
        var now = DateTime.UtcNow;
        var log = new ModerationLog
        {
            Id = new LogId(Guid.NewGuid()),
            CommunityId = communityId,
            Action = action,
            PerformedBy = performedBy,
            TargetUserId = targetUserId,
            TargetContent = targetContent,
            PerformedAt = now
        };
        log._domainEvents.Add(new ModerationActionLogged(log.Id, communityId, action, performedBy, targetUserId, targetContent, now));
        return log;
    }
}

