namespace Forum.Domain.Engines;

internal sealed class SpamDetectionEngine : ISpamDetectionEngine
{
    public bool IsSpam(string content, Guid userId)
    {
        // Placeholder: real implementation would use ML or heuristics
        return string.IsNullOrWhiteSpace(content) || content.Contains("spam", StringComparison.OrdinalIgnoreCase);
    }
}
