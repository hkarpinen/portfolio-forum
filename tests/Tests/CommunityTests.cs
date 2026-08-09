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
        var ownerId = new UserId(Guid.NewGuid());

        var community = Community.Create("TestCom", "testcom", CommunityVisibility.Public, ownerId);

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
        var community = CreateCommunity();

        Assert.Single(community.DomainEvents);
        Assert.IsType<CommunityCreated>(community.DomainEvents.First());
    }

    [Fact]
    public void Update_ShouldChangeNameAndVisibility()
    {
        var community = CreateCommunity();

        community.Update("NewName", "newname", CommunityVisibility.Private, DateTime.UtcNow);

        Assert.Equal("NewName", community.Name);
        Assert.Equal("newname", community.Slug);
        Assert.Equal(CommunityVisibility.Private, community.Visibility);
    }

    [Fact]
    public void Update_ShouldRaise_CommunityUpdatedEvent()
    {
        var community = CreateCommunity();

        community.Update("NewName", "newname", CommunityVisibility.Restricted, DateTime.UtcNow);

        Assert.Contains(community.DomainEvents, e => e is CommunityUpdated);
    }

    [Fact]
    public void Delete_ShouldRaise_CommunityDeletedEvent()
    {
        var community = CreateCommunity();

        community.Delete(DateTime.UtcNow);

        Assert.Contains(community.DomainEvents, e => e is CommunityDeleted);
    }

    [Fact]
    public void TransferOwnership_ShouldChangeOwner()
    {
        var community = CreateCommunity();
        var newOwner = new UserId(Guid.NewGuid());

        community.TransferOwnership(newOwner, DateTime.UtcNow);

        Assert.Equal(newOwner, community.OwnerId);
    }

    [Fact]
    public void TransferOwnership_ShouldRaise_OwnershipTransferredEvent()
    {
        var community = CreateCommunity();
        var newOwner = new UserId(Guid.NewGuid());

        community.TransferOwnership(newOwner, DateTime.UtcNow);

        Assert.Contains(community.DomainEvents, e => e is CommunityOwnershipTransferred);
    }
}
