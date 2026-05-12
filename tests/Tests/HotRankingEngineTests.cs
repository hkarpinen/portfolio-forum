using Forum.Domain.Engines;

namespace Tests;

public class HotRankingEngineTests
{
    [Fact]
    public void CalculateHotScore_HigherScore_ShouldRankHigher()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1);

        // Act
        var lowScore = HotRankingEngine.CalculateHotScore(createdAt, 1, 0);
        var highScore = HotRankingEngine.CalculateHotScore(createdAt, 100, 0);

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
        var oldScore = HotRankingEngine.CalculateHotScore(older, 10, 0);
        var newScore = HotRankingEngine.CalculateHotScore(newer, 10, 0);

        // Assert
        Assert.True(newScore > oldScore);
    }

    [Fact]
    public void CalculateHotScore_MoreComments_ShouldIncreaseScore()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1);

        // Act
        var noComments = HotRankingEngine.CalculateHotScore(createdAt, 10, 0);
        var manyComments = HotRankingEngine.CalculateHotScore(createdAt, 10, 100);

        // Assert
        Assert.True(manyComments > noComments);
    }

    [Fact]
    public void CalculateHotScore_ZeroScore_ShouldNotThrow()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1);

        // Act
        var score = HotRankingEngine.CalculateHotScore(createdAt, 0, 0);

        // Assert
        Assert.True(double.IsFinite(score));
    }

    [Fact]
    public void CalculateHotScore_NegativeScore_ShouldNotThrow()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1);

        // Act
        var score = HotRankingEngine.CalculateHotScore(createdAt, -5, 0);

        // Assert
        Assert.True(double.IsFinite(score));
    }
}
