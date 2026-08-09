using Forum.Domain.ValueObjects;
using Forum.Domain.Events;

namespace Forum.Domain.Aggregates;

public sealed class ForumThread : IAggregateRoot
{
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public ThreadId Id { get; private set; }
    public CommunityId CommunityId { get; private set; }
    public UserId AuthorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Content { get; private set; }

    /// <summary>Every public listing MUST filter on Published — drafts live in this same
    /// table, not a separate one.</summary>
    public ThreadStatus Status { get; private set; }
    /// <summary>Null for threads published in one step, never having been a draft.</summary>
    public DateTime? SavedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsPinned { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int VoteScore { get; private set; }

    private List<string> _tags = new();
    /// <summary>Up to 5, lowercased, ≤30 chars each.</summary>
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    private ForumThread() { }

    /// <summary>Published immediately, skipping the draft step.</summary>
    public static ForumThread Create(CommunityId communityId, string communitySlug, UserId authorId, string title, string? content, IReadOnlyList<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        var now = DateTime.UtcNow;
        var thread = new ForumThread
        {
            Id = new ThreadId(Guid.NewGuid()),
            CommunityId = communityId,
            AuthorId = authorId,
            Title = title,
            Content = content,
            _tags = NormaliseTags(tags),
            Status = ThreadStatus.Published,
            SavedAt = null,
            CreatedAt = now
        };
        thread._domainEvents.Add(new ThreadCreated(thread.Id, communityId, communitySlug, authorId, title, now));
        return thread;
    }

    /// <summary>
    /// Raises NO event — `ThreadCreated` signals public visibility and fires on publish.
    /// Empty titles are allowed here; validation happens at publish.
    /// </summary>
    public static ForumThread BeginDraft(CommunityId communityId, UserId authorId, string? title, string? content, IReadOnlyList<string>? tags = null)
    {
        var now = DateTime.UtcNow;
        return new ForumThread
        {
            Id = new ThreadId(Guid.NewGuid()),
            CommunityId = communityId,
            AuthorId = authorId,
            Title = title ?? string.Empty,
            Content = content,
            _tags = NormaliseTags(tags),
            Status = ThreadStatus.Draft,
            CreatedAt = now,
            SavedAt = now
        };
    }

    /// <summary>Ownership and state are enforced HERE, so no caller can skip them.</summary>
    public void Revise(UserId requestingAuthor, string? title, string? content, IReadOnlyList<string>? tags)
    {
        EnsureOwnedBy(requestingAuthor);
        if (Status != ThreadStatus.Draft)
            throw new InvalidOperationException("Only drafts can be revised; published threads use Edit.");

        Title = title ?? string.Empty;
        Content = content;
        _tags = NormaliseTags(tags);
        SavedAt = DateTime.UtcNow;
    }

    /// <summary>`CreatedAt` is OVERWRITTEN to the publication moment, so public
    /// timestamps show when it became visible, not when the editor was opened.</summary>
    public void Publish(UserId requestingAuthor, string communitySlug, DateTime publishedAt)
    {
        EnsureOwnedBy(requestingAuthor);
        if (Status != ThreadStatus.Draft)
            throw new InvalidOperationException("Thread is already published.");
        if (string.IsNullOrWhiteSpace(Title))
            throw new ArgumentException("A draft must have a title to be published.", nameof(Title));

        Status = ThreadStatus.Published;
        CreatedAt = publishedAt;
        SavedAt = null;
        _domainEvents.Add(new ThreadCreated(Id, CommunityId, communitySlug, AuthorId, Title, publishedAt));
    }

    /// <summary>For drafts only. Removing a published thread is a moderation action
    /// with different audit semantics.</summary>
    public void Abandon(UserId requestingAuthor, DateTime abandonedAt)
    {
        EnsureOwnedBy(requestingAuthor);
        if (Status != ThreadStatus.Draft)
            throw new InvalidOperationException("Use Delete to remove a published thread.");

        DeletedAt = abandonedAt;
    }

    public void EnsureOwnedBy(UserId requestingAuthor)
    {
        if (AuthorId != requestingAuthor)
            throw new InvalidOperationException("Thread does not belong to this user.");
    }

    public void Edit(string title, string? content, IReadOnlyList<string>? tags, DateTime editedAt)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot edit a deleted thread.");
        if (IsLocked)
            throw new InvalidOperationException("Cannot edit a locked thread.");
        if (Status != ThreadStatus.Published)
            throw new InvalidOperationException("Edit applies to published threads; drafts use Revise.");
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        Title = title;
        Content = content;
        _tags = NormaliseTags(tags);
        EditedAt = editedAt;
        _domainEvents.Add(new ThreadEdited(Id, title, content, editedAt));
    }

    private static List<string> NormaliseTags(IReadOnlyList<string>? tags)
    {
        if (tags is null || tags.Count == 0) return new List<string>();
        var normalised = tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLowerInvariant())
            .Distinct()
            .Take(5)
            .ToList();
        foreach (var tag in normalised)
            if (tag.Length > 30) throw new ArgumentException($"Tag '{tag}' exceeds 30 characters.", nameof(tags));
        return normalised;
    }

    public void Delete(DateTime deletedAt)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Thread is already deleted.");
        if (Status != ThreadStatus.Published)
            throw new InvalidOperationException("Delete applies to published threads; drafts use Abandon.");

        DeletedAt = deletedAt;
        _domainEvents.Add(new ThreadDeleted(Id, deletedAt));
    }

    public void Lock(DateTime lockedAt)
    {
        if (IsLocked)
            throw new InvalidOperationException("Thread is already locked.");

        IsLocked = true;
        _domainEvents.Add(new ThreadLocked(Id, lockedAt));
    }

    public void Pin(DateTime pinnedAt)
    {
        if (IsPinned)
            throw new InvalidOperationException("Thread is already pinned.");

        IsPinned = true;
        _domainEvents.Add(new ThreadPinned(Id, pinnedAt));
    }

    public void AdjustVoteScore(int delta)
    {
        VoteScore += delta;
    }
}
