namespace Forum.Domain.Engines;

public interface ISpamDetectionEngine
{
    bool IsSpam(string content, Guid userId);
}
