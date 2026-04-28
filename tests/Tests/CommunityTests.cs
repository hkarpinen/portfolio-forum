using Forum.Domain.Aggregates;
using Forum.Domain.Events;
using Forum.Domain.ValueObjects;

namespace Tests;

public class CommunityTests
{
    private static Community CreateCommunity(string name = "TestCommunity")
    {
        return Community.Create(name, Community.Slugify(name), CommunityVisibility.Public, new UserId(Guid.NewGuid()));
    }

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Arrange
        var ownerId = new UserId(Guid.NewGuid());

        // Act
        var community = Community.Create("TestCom", "testcom", CommunityVisibility.Public, ownerId);

        // Assert
        Assert.Equal("TestCom", community.Name);
        Assert.Equal("testcom", community.Slug);
        Assert.Equal(CommunityVisibility.Public, community.Visibility);
        Assert.Equal(ownerId, community.OwnerId);
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Community.Create("  ", "slug", CommunityVisibility.Public, new UserId(Guid.NewGuid())));
    }

    [Fact]
    public void Create_ShouldRaise_CommunityCreatedEvent()
    {
        // Arrange / Act
        var community = CreateCommunity();

        // Assert
        Assert.Single(community.DomainEvents);
        Assert.IsType<CommunityCreated>(community.DomainEvents.First());
    }

    [Fact]
    public void Update_ShouldChangeNameAndVisibility()
    {
        // Arrange
        var community = CreateCommunity();

        // Act
        community.Update("NewName", "newname", CommunityVisibility.Private, DateTime.UtcNow);

        // Assert
        Assert.Equal("NewName", community.Name);
        Assert.Equal("newname", community.Slug);
        Assert.Equal(CommunityVisibility.Private, community.Visibility);
    }

    [Fact]
    public void Update_ShouldRaise_CommunityUpdatedEvent()
    {
        // Arrange
        var community = CreateCommunity();

        // Act
        community.Update("NewName", "newname", CommunityVisibility.Restricted, DateTime.UtcNow);

        // Assert
        Assert.Contains(community.DomainEvents, e => e is CommunityUpdated);
    }

    [Fact]
    public void Delete_ShouldRaise_CommunityDeletedEvent()
    {
        // Arrange
        var community = CreateCommunity();

        // Act
        community.Delete(DateTime.UtcNow);

        // Assert
        Assert.Contains(community.DomainEvents, e => e is CommunityDeleted);
    }

    [Fact]
    public void TransferOwnership_ShouldChangeOwner()
    {
        // Arrange
        var community = CreateCommunity();
        var newOwner = new UserId(Guid.NewGuid());

        // Act
        community.TransferOwnership(newOwner, DateTime.UtcNow);

        // Assert
        Assert.Equal(newOwner, community.OwnerId);
    }

    [Fact]
    public void TransferOwnership_ShouldRaise_OwnershipTransferredEvent()
    {
        // Arrange
        var community = CreateCommunity();
        var newOwner = new UserId(Guid.NewGuid());

        // Act
        community.TransferOwnership(newOwner, DateTime.UtcNow);

        // Assert
        Assert.Contains(community.DomainEvents, e => e is CommunityOwnershipTransferred);
    }
}
