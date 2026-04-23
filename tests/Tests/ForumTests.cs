using Forum.Domain.Engines;
using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Forum.Domain.Events;

namespace Tests;

public class HotRankingEngineTests
{
    private readonly IHotRankingEngine _engine = new HotRankingEngine();

    [Fact]
    public void CalculateHotScore_HigherScore_ShouldRankHigher()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1);

        // Act
        var lowScore = _engine.CalculateHotScore(createdAt, 1, 0);
        var highScore = _engine.CalculateHotScore(createdAt, 100, 0);

        // Assert
        Assert.True(highScore > lowScore);
    }

    [Fact]
    public void CalculateHotScore_NewerPost_ShouldRankHigher_WhenScoresEqual()
    {
        // Arrange
        var older = new DateTime(2020, 1, 1);
        var newer = new DateTime(2024, 1, 1);

        // Act
        var oldScore = _engine.CalculateHotScore(older, 10, 0);
        var newScore = _engine.CalculateHotScore(newer, 10, 0);

        // Assert
        Assert.True(newScore > oldScore);
    }

    [Fact]
    public void CalculateHotScore_MoreComments_ShouldIncreaseScore()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1);

        // Act
        var noComments = _engine.CalculateHotScore(createdAt, 10, 0);
        var manyComments = _engine.CalculateHotScore(createdAt, 10, 100);

        // Assert
        Assert.True(manyComments > noComments);
    }

    [Fact]
    public void CalculateHotScore_ZeroScore_ShouldNotThrow()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1);

        // Act
        var score = _engine.CalculateHotScore(createdAt, 0, 0);

        // Assert
        Assert.True(double.IsFinite(score));
    }

    [Fact]
    public void CalculateHotScore_NegativeScore_ShouldNotThrow()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1);

        // Act
        var score = _engine.CalculateHotScore(createdAt, -5, 0);

        // Assert
        Assert.True(double.IsFinite(score));
    }
}

public class SpamDetectionEngineTests
{
    private readonly ISpamDetectionEngine _engine = new SpamDetectionEngine();

    [Fact]
    public void IsSpam_NormalContent_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = _engine.IsSpam("Hello, this is a normal post.", userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSpam_ContentContainsSpam_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = _engine.IsSpam("Buy cheap spam now!", userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSpam_EmptyContent_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = _engine.IsSpam("", userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSpam_WhitespaceContent_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = _engine.IsSpam("   ", userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSpam_ContentContainsSpam_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = _engine.IsSpam("SPAM SPAM SPAM", userId);

        // Assert
        Assert.True(result);
    }
}

public class ForumThreadTests
{
    private static ForumThread CreateThread(string title = "Test Thread", string? content = "Some content")
    {
        return ForumThread.Create(
            new CommunityId(Guid.NewGuid()),
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
        var thread = ForumThread.Create(communityId, authorId, "Title", "Content");

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
            ForumThread.Create(new CommunityId(Guid.NewGuid()), new UserId(Guid.NewGuid()), "  ", null));
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

public class CommunityTests
{
    private static Community CreateCommunity(string name = "TestCommunity")
    {
        return Community.Create(name, CommunityVisibility.Public, new UserId(Guid.NewGuid()));
    }

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Arrange
        var ownerId = new UserId(Guid.NewGuid());

        // Act
        var community = Community.Create("TestCom", CommunityVisibility.Public, ownerId);

        // Assert
        Assert.Equal("TestCom", community.Name);
        Assert.Equal(CommunityVisibility.Public, community.Visibility);
        Assert.Equal(ownerId, community.OwnerId);
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Community.Create("  ", CommunityVisibility.Public, new UserId(Guid.NewGuid())));
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
        community.Update("NewName", CommunityVisibility.Private, DateTime.UtcNow);

        // Assert
        Assert.Equal("NewName", community.Name);
        Assert.Equal(CommunityVisibility.Private, community.Visibility);
    }

    [Fact]
    public void Update_ShouldRaise_CommunityUpdatedEvent()
    {
        // Arrange
        var community = CreateCommunity();

        // Act
        community.Update("NewName", CommunityVisibility.Restricted, DateTime.UtcNow);

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

public class VoteTests
{
    private static Vote CreateVote(VoteDirection direction = VoteDirection.Upvote)
        => Vote.Create(VoteTargetType.Thread, Guid.NewGuid(), new UserId(Guid.NewGuid()), direction);

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var userId = new UserId(Guid.NewGuid());

        // Act
        var vote = Vote.Create(VoteTargetType.Comment, targetId, userId, VoteDirection.Downvote);

        // Assert
        Assert.Equal(VoteTargetType.Comment, vote.TargetType);
        Assert.Equal(targetId, vote.TargetId);
        Assert.Equal(userId, vote.UserId);
        Assert.Equal(VoteDirection.Downvote, vote.Direction);
        Assert.Null(vote.RetractedAt);
    }

    [Fact]
    public void Create_ShouldRaise_VoteCastEvent()
    {
        var vote = CreateVote();
        Assert.Single(vote.DomainEvents);
        Assert.IsType<VoteCast>(vote.DomainEvents.First());
    }

    [Fact]
    public void Retract_ShouldSetRetractedAt()
    {
        var vote = CreateVote();
        var retractedAt = DateTime.UtcNow.AddMinutes(10);
        vote.Retract(retractedAt);
        Assert.Equal(retractedAt, vote.RetractedAt);
    }

    [Fact]
    public void Retract_ShouldRaise_VoteRetractedEvent()
    {
        var vote = CreateVote();
        vote.Retract(DateTime.UtcNow);
        Assert.Contains(vote.DomainEvents, e => e is VoteRetracted);
    }

    [Fact]
    public void Retract_WhenAlreadyRetracted_ShouldThrow()
    {
        var vote = CreateVote();
        vote.Retract(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => vote.Retract(DateTime.UtcNow));
    }

    [Fact]
    public void SwitchDirection_ShouldFlipDirection()
    {
        var vote = CreateVote(VoteDirection.Upvote);
        vote.SwitchDirection(VoteDirection.Downvote, DateTime.UtcNow);
        Assert.Equal(VoteDirection.Downvote, vote.Direction);
    }

    [Fact]
    public void SwitchDirection_ShouldRaise_VoteSwitchedEvent()
    {
        var vote = CreateVote(VoteDirection.Upvote);
        vote.SwitchDirection(VoteDirection.Downvote, DateTime.UtcNow);
        Assert.Contains(vote.DomainEvents, e => e is VoteSwitched);
    }

    [Fact]
    public void SwitchDirection_SameDirection_ShouldThrow()
    {
        var vote = CreateVote(VoteDirection.Upvote);
        Assert.Throws<InvalidOperationException>(() => vote.SwitchDirection(VoteDirection.Upvote, DateTime.UtcNow));
    }
}

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
        Assert.Null(membership.LeftAt);
    }

    [Fact]
    public void Create_ShouldRaise_MembershipJoinedEvent()
    {
        var membership = CreateMembership();
        Assert.Single(membership.DomainEvents);
        Assert.IsType<MembershipJoined>(membership.DomainEvents.First());
    }

    [Fact]
    public void Leave_ShouldSetLeftAt_AndRaiseEvent()
    {
        var membership = CreateMembership();
        var leftAt = DateTime.UtcNow;
        membership.Leave(leftAt);
        Assert.Equal(leftAt, membership.LeftAt);
        Assert.Contains(membership.DomainEvents, e => e is MembershipLeft);
    }

    [Fact]
    public void Leave_WhenAlreadyLeft_ShouldThrow()
    {
        var membership = CreateMembership();
        membership.Leave(DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => membership.Leave(DateTime.UtcNow));
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
