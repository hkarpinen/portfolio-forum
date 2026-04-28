using Forum.Domain.Aggregates;
using Forum.Domain.Events;
using Forum.Domain.ValueObjects;

namespace Tests;

public class ForumThreadTests
{
    private static ForumThread CreateThread(string title = "Test Thread", string? content = "Some content")
    {
        return ForumThread.Create(
            new CommunityId(Guid.NewGuid()),
            "test-community",
            new UserId(Guid.NewGuid()),
            title,
            content);
    }

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Arrange
        var communityId = new CommunityId(Guid.NewGuid());
        var authorId = new UserId(Guid.NewGuid());

        // Act
        var thread = ForumThread.Create(communityId, "test-community", authorId, "Title", "Content");

        // Assert
        Assert.Equal(communityId, thread.CommunityId);
        Assert.Equal(authorId, thread.AuthorId);
        Assert.Equal("Title", thread.Title);
        Assert.Equal("Content", thread.Content);
        Assert.False(thread.IsLocked);
        Assert.False(thread.IsPinned);
        Assert.Null(thread.DeletedAt);
    }

    [Fact]
    public void Create_EmptyTitle_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ForumThread.Create(new CommunityId(Guid.NewGuid()), "test-community", new UserId(Guid.NewGuid()), "  ", null));
    }

    [Fact]
    public void Create_ShouldRaise_ThreadCreatedEvent()
    {
        // Arrange / Act
        var thread = CreateThread();

        // Assert
        Assert.Single(thread.DomainEvents);
        Assert.IsType<ThreadCreated>(thread.DomainEvents.First());
    }

    [Fact]
    public void Edit_ShouldUpdateTitleAndContent()
    {
        // Arrange
        var thread = CreateThread();

        // Act
        thread.Edit("New Title", "New Content", DateTime.UtcNow);

        // Assert
        Assert.Equal("New Title", thread.Title);
        Assert.Equal("New Content", thread.Content);
        Assert.NotNull(thread.EditedAt);
    }

    [Fact]
    public void Edit_ShouldRaise_ThreadEditedEvent()
    {
        // Arrange
        var thread = CreateThread();

        // Act
        thread.Edit("New Title", "Content", DateTime.UtcNow);

        // Assert
        Assert.Contains(thread.DomainEvents, e => e is ThreadEdited);
    }

    [Fact]
    public void Lock_ShouldSetIsLocked()
    {
        // Arrange
        var thread = CreateThread();

        // Act
        thread.Lock(DateTime.UtcNow);

        // Assert
        Assert.True(thread.IsLocked);
    }

    [Fact]
    public void Lock_ShouldRaise_ThreadLockedEvent()
    {
        // Arrange
        var thread = CreateThread();

        // Act
        thread.Lock(DateTime.UtcNow);

        // Assert
        Assert.Contains(thread.DomainEvents, e => e is ThreadLocked);
    }

    [Fact]
    public void Delete_ShouldSetDeletedAt()
    {
        // Arrange
        var thread = CreateThread();
        var deletedAt = DateTime.UtcNow;

        // Act
        thread.Delete(deletedAt);

        // Assert
        Assert.Equal(deletedAt, thread.DeletedAt);
    }

    [Fact]
    public void Lock_AlreadyLocked_ShouldThrow()
    {
        var thread = CreateThread();
        thread.Lock(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => thread.Lock(DateTime.UtcNow));
    }

    [Fact]
    public void Pin_ShouldSetIsPinned()
    {
        var thread = CreateThread();
        thread.Pin(DateTime.UtcNow);
        Assert.True(thread.IsPinned);
    }

    [Fact]
    public void Pin_AlreadyPinned_ShouldThrow()
    {
        var thread = CreateThread();
        thread.Pin(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => thread.Pin(DateTime.UtcNow));
    }

    [Fact]
    public void Edit_WhenLocked_ShouldThrow()
    {
        var thread = CreateThread();
        thread.Lock(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => thread.Edit("New", null, DateTime.UtcNow));
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldThrow()
    {
        var thread = CreateThread();
        thread.Delete(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => thread.Delete(DateTime.UtcNow));
    }
}
