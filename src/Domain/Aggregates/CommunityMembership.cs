using Forum.Domain.ValueObjects;
using Forum.Domain.Events;

namespace Forum.Domain.Aggregates;

public sealed class CommunityMembership
{
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public MembershipId Id { get; private set; }
    public CommunityId CommunityId { get; private set; }
    public UserId UserId { get; private set; }
    public CommunityRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LeftAt { get; private set; }

    private CommunityMembership() { }

    public static CommunityMembership Create(CommunityId communityId, UserId userId, CommunityRole role = CommunityRole.Member)
    {
        var membership = new CommunityMembership
        {
            Id = new MembershipId(Guid.NewGuid()),
            CommunityId = communityId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
        membership._domainEvents.Add(new MembershipJoined(membership.Id, communityId, userId, membership.JoinedAt));
        return membership;
    }

    public void Leave(DateTime leftAt)
    {
        if (LeftAt.HasValue)
            throw new InvalidOperationException("Membership is already left.");

        LeftAt = leftAt;
        _domainEvents.Add(new MembershipLeft(Id, CommunityId, UserId, leftAt));
    }

    public void AppointModerator(DateTime appointedAt)
    {
        if (Role == CommunityRole.Moderator)
            throw new InvalidOperationException("Member is already a moderator.");

        Role = CommunityRole.Moderator;
        _domainEvents.Add(new ModeratorAppointed(CommunityId, UserId, appointedAt));
    }

    public void RemoveModerator(DateTime removedAt)
    {
        if (Role != CommunityRole.Moderator)
            throw new InvalidOperationException("Member is not a moderator.");

        Role = CommunityRole.Member;
        _domainEvents.Add(new ModeratorRemoved(CommunityId, UserId, removedAt));
    }
}

