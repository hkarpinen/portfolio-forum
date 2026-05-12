using Forum.Domain.Engines;

namespace Tests;

public class SpamDetectionEngineTests
{
    [Fact]
    public void IsSpam_NormalContent_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = SpamDetectionEngine.IsSpam("Hello, this is a normal post.", userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSpam_ContentContainsSpam_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = SpamDetectionEngine.IsSpam("Buy cheap spam now!", userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSpam_EmptyContent_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = SpamDetectionEngine.IsSpam("", userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSpam_WhitespaceContent_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = SpamDetectionEngine.IsSpam("   ", userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSpam_ContentContainsSpam_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = SpamDetectionEngine.IsSpam("SPAM SPAM SPAM", userId);

        // Assert
        Assert.True(result);
    }
}
