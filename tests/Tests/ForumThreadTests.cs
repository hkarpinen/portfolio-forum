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
        var communityId = new CommunityId(Guid.NewGuid());
        var authorId = new UserId(Guid.NewGuid());

        var thread = ForumThread.Create(communityId, "test-community", authorId, "Title", "Content");

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
        var thread = CreateThread();

        Assert.Single(thread.DomainEvents);
        Assert.IsType<ThreadCreated>(thread.DomainEvents.First());
    }

    [Fact]
    public void Edit_ShouldUpdateTitleAndContent()
    {
        var thread = CreateThread();

        thread.Edit("New Title", "New Content", null, DateTime.UtcNow);

        Assert.Equal("New Title", thread.Title);
        Assert.Equal("New Content", thread.Content);
        Assert.NotNull(thread.EditedAt);
    }

    [Fact]
    public void Edit_ShouldRaise_ThreadEditedEvent()
    {
        var thread = CreateThread();

        thread.Edit("New Title", "Content", null, DateTime.UtcNow);

        Assert.Contains(thread.DomainEvents, e => e is ThreadEdited);
    }

    [Fact]
    public void Lock_ShouldSetIsLocked()
    {
        var thread = CreateThread();

        thread.Lock(DateTime.UtcNow);

        Assert.True(thread.IsLocked);
    }

    [Fact]
    public void Lock_ShouldRaise_ThreadLockedEvent()
    {
        var thread = CreateThread();

        thread.Lock(DateTime.UtcNow);

        Assert.Contains(thread.DomainEvents, e => e is ThreadLocked);
    }

    [Fact]
    public void Delete_ShouldSetDeletedAt()
    {
        var thread = CreateThread();
        var deletedAt = DateTime.UtcNow;

        thread.Delete(deletedAt);

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
        Assert.Throws<InvalidOperationException>(() => thread.Edit("New", null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldThrow()
    {
        var thread = CreateThread();
        thread.Delete(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => thread.Delete(DateTime.UtcNow));
    }
}
