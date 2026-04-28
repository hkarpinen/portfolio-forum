using Forum.Domain.Aggregates;
using Forum.Domain.Events;
using Forum.Domain.ValueObjects;

namespace Tests;

public class CommunityBanTests
{
    private static CommunityBan CreateBan(string? reason = "Spamming")
        => CommunityBan.Create(new CommunityId(Guid.NewGuid()), new UserId(Guid.NewGuid()), reason);

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Arrange
        var communityId = new CommunityId(Guid.NewGuid());
        var userId = new UserId(Guid.NewGuid());

        // Act
        var ban = CommunityBan.Create(communityId, userId, "Spamming");

        // Assert
        Assert.Equal(communityId, ban.CommunityId);
        Assert.Equal(userId, ban.UserId);
        Assert.Equal("Spamming", ban.Reason);
        Assert.Null(ban.UnbannedAt);
    }

    [Fact]
    public void Create_ShouldRaise_UserBannedEvent()
    {
        var ban = CreateBan();
        Assert.Single(ban.DomainEvents);
        Assert.IsType<UserBanned>(ban.DomainEvents.First());
    }

    [Fact]
    public void Unban_ShouldSetUnbannedAt()
    {
        var ban = CreateBan();
        var unbannedAt = DateTime.UtcNow.AddDays(1);
        ban.Unban(unbannedAt);
        Assert.Equal(unbannedAt, ban.UnbannedAt);
    }

    [Fact]
    public void Unban_ShouldRaise_UserUnbannedEvent()
    {
        var ban = CreateBan();
        ban.Unban(DateTime.UtcNow.AddDays(1));
        Assert.Contains(ban.DomainEvents, e => e is UserUnbanned);
    }

    [Fact]
    public void Unban_AlreadyUnbanned_ShouldThrow()
    {
        var ban = CreateBan();
        ban.Unban(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => ban.Unban(DateTime.UtcNow));
    }
}
