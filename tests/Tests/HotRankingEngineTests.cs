using Forum.Domain.Engines;

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
