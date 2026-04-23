using Forum.Domain.ValueObjects;
using Forum.Domain.Events;

namespace Forum.Domain.Aggregates;

public sealed class Comment
{
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public CommentId Id { get; private set; }
    public ThreadId ThreadId { get; private set; }
    public UserId AuthorId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Comment() { }

    public static Comment Create(ThreadId threadId, UserId authorId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty.", nameof(content));

        var comment = new Comment
        {
            Id = new CommentId(Guid.NewGuid()),
            ThreadId = threadId,
            AuthorId = authorId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        comment._domainEvents.Add(new CommentCreated(comment.Id, threadId, authorId, content, comment.CreatedAt));
        return comment;
    }

    public void Edit(string content, DateTime editedAt)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot edit a deleted comment.");
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty.", nameof(content));

        Content = content;
        EditedAt = editedAt;
        _domainEvents.Add(new CommentEdited(Id, content, editedAt));
    }

    public void Delete(DateTime deletedAt)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Comment is already deleted.");

        DeletedAt = deletedAt;
        _domainEvents.Add(new CommentDeleted(Id, deletedAt));
    }
}

