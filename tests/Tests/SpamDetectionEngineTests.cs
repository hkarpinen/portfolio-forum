using Forum.Domain.Engines;

namespace Tests;

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
