namespace Forum.Domain;

public interface IAggregateRoot
{
    IReadOnlyCollection<object> DomainEvents { get; }
    void ClearDomainEvents();
}
