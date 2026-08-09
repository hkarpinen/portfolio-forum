using Forum.Domain.Engines;

namespace Tests;

public class SpamDetectionEngineTests
{
    [Fact]
    public void IsSpam_NormalContent_ReturnsFalse()
    {
        var userId = Guid.NewGuid();

        var result = SpamDetectionEngine.IsSpam("Hello, this is a normal post.", userId);

        Assert.False(result);
    }

    [Fact]
    public void IsSpam_ContentContainsSpam_ReturnsTrue()
    {
        var userId = Guid.NewGuid();

        var result = SpamDetectionEngine.IsSpam("Buy cheap spam now!", userId);

        Assert.True(result);
    }

    [Fact]
    public void IsSpam_EmptyContent_ReturnsTrue()
    {
        var userId = Guid.NewGuid();

        var result = SpamDetectionEngine.IsSpam("", userId);

        Assert.True(result);
    }

    [Fact]
    public void IsSpam_WhitespaceContent_ReturnsTrue()
    {
        var userId = Guid.NewGuid();

        var result = SpamDetectionEngine.IsSpam("   ", userId);

        Assert.True(result);
    }

    [Fact]
    public void IsSpam_ContentContainsSpam_CaseInsensitive_ReturnsTrue()
    {
        var userId = Guid.NewGuid();

        var result = SpamDetectionEngine.IsSpam("SPAM SPAM SPAM", userId);

        Assert.True(result);
    }
}
