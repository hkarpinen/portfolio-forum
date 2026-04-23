using Forum.Domain.ValueObjects;
using Forum.Domain.Events;

namespace Forum.Domain.Aggregates;

public sealed class CommunityBan
{
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public BanId Id { get; private set; }
    public CommunityId CommunityId { get; private set; }
    public UserId UserId { get; private set; }
    public DateTime BannedAt { get; private set; }
    public string? Reason { get; private set; }
    public DateTime? UnbannedAt { get; private set; }

    private CommunityBan() { }

    public static CommunityBan Create(CommunityId communityId, UserId userId, string? reason)
    {
        var now = DateTime.UtcNow;
        var ban = new CommunityBan
        {
            Id = new BanId(Guid.NewGuid()),
            CommunityId = communityId,
            UserId = userId,
            BannedAt = now,
            Reason = reason
        };
        ban._domainEvents.Add(new UserBanned(ban.Id, communityId, userId, now, reason));
        return ban;
    }

    public void Unban(DateTime unbannedAt)
    {
        if (UnbannedAt.HasValue)
            throw new InvalidOperationException("User is already unbanned.");

        UnbannedAt = unbannedAt;
        _domainEvents.Add(new UserUnbanned(Id, CommunityId, UserId, unbannedAt));
    }
}

