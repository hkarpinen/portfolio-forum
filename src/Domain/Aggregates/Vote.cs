using Forum.Domain.ValueObjects;
using Forum.Domain.Events;

namespace Forum.Domain.Aggregates;

public sealed class Vote : IAggregateRoot
{
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public VoteId Id { get; private set; }
    public VoteTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public UserId UserId { get; private set; }
    public VoteDirection Direction { get; private set; }
    public DateTime CastAt { get; private set; }

    private Vote() { }

    public static Vote Create(VoteTargetType targetType, Guid targetId, UserId userId, VoteDirection direction)
    {
        var vote = new Vote
        {
            Id = new VoteId(Guid.NewGuid()),
            TargetType = targetType,
            TargetId = targetId,
            UserId = userId,
            Direction = direction,
            CastAt = DateTime.UtcNow
        };
        vote._domainEvents.Add(new VoteCast(vote.Id, targetType, targetId, userId, direction, vote.CastAt));
        return vote;
    }

    public void SwitchDirection(VoteDirection newDirection, DateTime switchedAt)
    {
        if (newDirection == Direction)
            throw new InvalidOperationException("Vote is already in this direction.");

        var oldDirection = Direction;
        Direction = newDirection;
        _domainEvents.Add(new VoteSwitched(Id, TargetType, TargetId, UserId, oldDirection, newDirection, switchedAt));
    }
}

