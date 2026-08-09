using Forum.Domain.Aggregates;
using Forum.Domain.Events;
using Forum.Domain.ValueObjects;

namespace Tests;

public class VoteTests
{
    private static Vote CreateVote(VoteDirection direction = VoteDirection.Upvote)
        => Vote.Create(VoteTargetType.Thread, Guid.NewGuid(), new UserId(Guid.NewGuid()), direction);

    [Fact]
    public void Create_ShouldSetProperties()
    {
        var targetId = Guid.NewGuid();
        var userId = new UserId(Guid.NewGuid());

        var vote = Vote.Create(VoteTargetType.Comment, targetId, userId, VoteDirection.Downvote);

        Assert.Equal(VoteTargetType.Comment, vote.TargetType);
        Assert.Equal(targetId, vote.TargetId);
        Assert.Equal(userId, vote.UserId);
        Assert.Equal(VoteDirection.Downvote, vote.Direction);
    }

    [Fact]
    public void Create_ShouldRaise_VoteCastEvent()
    {
        var vote = CreateVote();
        Assert.Single(vote.DomainEvents);
        Assert.IsType<VoteCast>(vote.DomainEvents.First());
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
