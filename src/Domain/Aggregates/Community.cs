using Forum.Domain.ValueObjects;
using Forum.Domain.Events;

namespace Forum.Domain.Aggregates;

public sealed class Community
{
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public CommunityId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public CommunityVisibility Visibility { get; private set; }
    public UserId OwnerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Community() { }

    public static Community Create(string name, CommunityVisibility visibility, UserId ownerId, string? description = null, string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        var community = new Community
        {
            Id = new CommunityId(Guid.NewGuid()),
            Name = name,
            Description = description,
            ImageUrl = imageUrl,
            Visibility = visibility,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };
        community._domainEvents.Add(new CommunityCreated(community.Id, name, visibility, ownerId, community.CreatedAt));
        return community;
    }

    public void Update(string name, CommunityVisibility visibility, DateTime updatedAt, string? description = null, string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Name = name;
        Description = description;
        ImageUrl = imageUrl;
        Visibility = visibility;
        UpdatedAt = updatedAt;
        _domainEvents.Add(new CommunityUpdated(Id, name, visibility, updatedAt));
    }

    public void Delete(DateTime deletedAt)
        => _domainEvents.Add(new CommunityDeleted(Id, deletedAt));

    public void TransferOwnership(UserId newOwnerId, DateTime transferredAt)
    {
        var previousOwner = OwnerId;
        OwnerId = newOwnerId;
        _domainEvents.Add(new CommunityOwnershipTransferred(Id, previousOwner, newOwnerId, transferredAt));
    }
}

