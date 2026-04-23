using Forum.Domain.ValueObjects;
using Forum.Domain.Events;

namespace Forum.Domain.Aggregates;

public sealed class ForumThread
{
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public ThreadId Id { get; private set; }
    public CommunityId CommunityId { get; private set; }
    public UserId AuthorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Content { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsPinned { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private ForumThread() { }

    public static ForumThread Create(CommunityId communityId, UserId authorId, string title, string? content)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        var thread = new ForumThread
        {
            Id = new ThreadId(Guid.NewGuid()),
            CommunityId = communityId,
            AuthorId = authorId,
            Title = title,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        thread._domainEvents.Add(new ThreadCreated(thread.Id, communityId, authorId, title, thread.CreatedAt));
        return thread;
    }

    public void Edit(string title, string? content, DateTime editedAt)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot edit a deleted thread.");
        if (IsLocked)
            throw new InvalidOperationException("Cannot edit a locked thread.");
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));

        Title = title;
        Content = content;
        EditedAt = editedAt;
        _domainEvents.Add(new ThreadEdited(Id, title, content, editedAt));
    }

    public void Delete(DateTime deletedAt)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Thread is already deleted.");

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
}

