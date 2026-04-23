using Forum.Domain.Events;
using Forum.Domain.ValueObjects;

namespace Forum.Domain.Aggregates;

public sealed class ForumProfile
{
    public const int MaxBioLength = 500;
    public const int MaxSignatureLength = 200;

    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public UserId UserId { get; private set; } = null!;
    public string? Bio { get; private set; }
    public string? Signature { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private ForumProfile() { }

    public static ForumProfile Create(UserId userId, string? bio, string? signature)
    {
        Validate(bio, signature);

        var now = DateTime.UtcNow;
        var profile = new ForumProfile
        {
            UserId = userId,
            Bio = Normalize(bio),
            Signature = Normalize(signature),
            CreatedAt = now
        };

        profile._domainEvents.Add(new ForumProfileUpdated(userId, profile.Bio, profile.Signature, now));
        return profile;
    }

    public void Update(string? bio, string? signature, DateTime updatedAt)
    {
        Validate(bio, signature);

        Bio = Normalize(bio);
        Signature = Normalize(signature);
        UpdatedAt = updatedAt;

        _domainEvents.Add(new ForumProfileUpdated(UserId, Bio, Signature, updatedAt));
    }

    private static void Validate(string? bio, string? signature)
    {
        if (bio is { Length: > MaxBioLength })
            throw new ArgumentException($"Bio cannot exceed {MaxBioLength} characters.", nameof(bio));

        if (signature is { Length: > MaxSignatureLength })
            throw new ArgumentException($"Signature cannot exceed {MaxSignatureLength} characters.", nameof(signature));
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
