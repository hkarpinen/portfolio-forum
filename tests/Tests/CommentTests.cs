using Forum.Domain.Aggregates;
using Forum.Domain.Events;
using Forum.Domain.ValueObjects;

namespace Tests;

public class CommentTests
{
    private static Comment CreateComment(string content = "A comment")
        => Comment.Create(new ThreadId(Guid.NewGuid()), new UserId(Guid.NewGuid()), content);

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Arrange
        var threadId = new ThreadId(Guid.NewGuid());
        var authorId = new UserId(Guid.NewGuid());

        // Act
        var comment = Comment.Create(threadId, authorId, "Hello");

        // Assert
        Assert.Equal(threadId, comment.ThreadId);
        Assert.Equal(authorId, comment.AuthorId);
        Assert.Equal("Hello", comment.Content);
        Assert.Null(comment.EditedAt);
        Assert.Null(comment.DeletedAt);
    }

    [Fact]
    public void Create_EmptyContent_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Comment.Create(new ThreadId(Guid.NewGuid()), new UserId(Guid.NewGuid()), "   "));
    }

    [Fact]
    public void Create_ShouldRaise_CommentCreatedEvent()
    {
        var comment = CreateComment();
        Assert.Single(comment.DomainEvents);
        Assert.IsType<CommentCreated>(comment.DomainEvents.First());
    }

    [Fact]
    public void Edit_ShouldUpdateContentAndEditedAt()
    {
        // Arrange
        var comment = CreateComment();
        var editedAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        comment.Edit("Updated", editedAt);

        // Assert
        Assert.Equal("Updated", comment.Content);
        Assert.Equal(editedAt, comment.EditedAt);
    }

    [Fact]
    public void Edit_ShouldRaise_CommentEditedEvent()
    {
        var comment = CreateComment();
        comment.Edit("Updated", DateTime.UtcNow);
        Assert.Contains(comment.DomainEvents, e => e is CommentEdited);
    }

    [Fact]
    public void Edit_EmptyContent_ShouldThrow()
    {
        var comment = CreateComment();
        Assert.Throws<ArgumentException>(() => comment.Edit("  ", DateTime.UtcNow));
    }

    [Fact]
    public void Edit_WhenDeleted_ShouldThrow()
    {
        var comment = CreateComment();
        comment.Delete(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => comment.Edit("Updated", DateTime.UtcNow));
    }

    [Fact]
    public void Delete_ShouldSetDeletedAt_AndRaiseEvent()
    {
        var comment = CreateComment();
        var deletedAt = DateTime.UtcNow;
        comment.Delete(deletedAt);
        Assert.Equal(deletedAt, comment.DeletedAt);
        Assert.Contains(comment.DomainEvents, e => e is CommentDeleted);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldThrow()
    {
        var comment = CreateComment();
        comment.Delete(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => comment.Delete(DateTime.UtcNow));
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyCollection()
    {
        var comment = CreateComment();
        comment.ClearDomainEvents();
        Assert.Empty(comment.DomainEvents);
    }
}
