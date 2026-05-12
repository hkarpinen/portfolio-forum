using System.Text;
using Forum.Domain.ValueObjects;
using Forum.Domain.Events;

namespace Forum.Domain.Aggregates;

public sealed class Community : IAggregateRoot
{
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public CommunityId Id { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public CommunityVisibility Visibility { get; private set; }
    public UserId OwnerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Community() { }

    public static Community Create(
        string name,
        string slug,
        CommunityVisibility visibility,
        UserId ownerId,
        string? description = null,
        string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be empty.", nameof(slug));

        var community = new Community
        {
            Id = new CommunityId(Guid.NewGuid()),
            Slug = slug,
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

    public void Update(
        string name,
        string slug,
        CommunityVisibility visibility,
        DateTime updatedAt,
        string? description = null,
        string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug cannot be empty.", nameof(slug));

        Name = name;
        Slug = slug;
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

    /// <summary>
    /// Produce a URL-safe base slug from a display name. Lowercase ASCII alphanum + hyphens only.
    /// Strips diacritics, collapses whitespace/punctuation to single hyphens, trims trailing hyphens.
    /// Returns empty string if the name has no slug-able characters (caller must handle fallback).
    /// </summary>
    public static string Slugify(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        // Normalize diacritics: "café" -> "cafe"
        var normalized = name.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch);
        }

        var lowered = sb.ToString().ToLowerInvariant();
        var result = new StringBuilder(lowered.Length);
        var lastWasHyphen = true; // skip leading hyphens
        foreach (var ch in lowered)
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
            {
                result.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen)
            {
                result.Append('-');
                lastWasHyphen = true;
            }
        }

        return result.ToString().TrimEnd('-');
    }
}

