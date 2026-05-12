namespace Forum.Domain;

/// <summary>
/// Marker interface for domain aggregates that raise domain events.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<object> DomainEvents { get; }
    void ClearDomainEvents();
}
