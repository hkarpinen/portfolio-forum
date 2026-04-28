using Forum.Domain.Aggregates;
using Forum.Domain.Events;
using Forum.Domain.ValueObjects;

namespace Tests;

public class CommunityMembershipTests
{
    private static CommunityMembership CreateMembership(CommunityRole role = CommunityRole.Member)
        => CommunityMembership.Create(new CommunityId(Guid.NewGuid()), new UserId(Guid.NewGuid()), role);

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Arrange
        var communityId = new CommunityId(Guid.NewGuid());
        var userId = new UserId(Guid.NewGuid());

        // Act
        var membership = CommunityMembership.Create(communityId, userId);

        // Assert
        Assert.Equal(communityId, membership.CommunityId);
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(CommunityRole.Member, membership.Role);
    }

    [Fact]
    public void Create_ShouldRaise_MembershipJoinedEvent()
    {
        var membership = CreateMembership();
        Assert.Single(membership.DomainEvents);
        Assert.IsType<MembershipJoined>(membership.DomainEvents.First());
    }

    [Fact]
    public void AppointModerator_ShouldChangeRole_AndRaiseEvent()
    {
        var membership = CreateMembership();
        membership.AppointModerator(DateTime.UtcNow);
        Assert.Equal(CommunityRole.Moderator, membership.Role);
        Assert.Contains(membership.DomainEvents, e => e is ModeratorAppointed);
    }

    [Fact]
    public void AppointModerator_WhenAlreadyModerator_ShouldThrow()
    {
        var membership = CreateMembership(CommunityRole.Moderator);
        Assert.Throws<InvalidOperationException>(() => membership.AppointModerator(DateTime.UtcNow));
    }

    [Fact]
    public void RemoveModerator_ShouldDemoteToMember_AndRaiseEvent()
    {
        var membership = CreateMembership(CommunityRole.Moderator);
        membership.RemoveModerator(DateTime.UtcNow);
        Assert.Equal(CommunityRole.Member, membership.Role);
        Assert.Contains(membership.DomainEvents, e => e is ModeratorRemoved);
    }

    [Fact]
    public void RemoveModerator_WhenNotModerator_ShouldThrow()
    {
        var membership = CreateMembership(CommunityRole.Member);
        Assert.Throws<InvalidOperationException>(() => membership.RemoveModerator(DateTime.UtcNow));
    }
}
