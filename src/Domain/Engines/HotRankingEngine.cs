using System;

namespace Forum.Domain.Engines;

internal sealed class HotRankingEngine : IHotRankingEngine
{
    public double CalculateHotScore(DateTime createdAt, int score, int commentCount)
    {
        // Engagement boost: votes count heavily, comments add a bonus
        var engagementBoost = score * 2.0 + commentCount * 1.5;

        // Age decay: threads lose ~1 point per 6 hours of age
        // Using seconds since a fixed epoch to keep numbers positive
        var ageSeconds = (DateTime.UtcNow - createdAt).TotalSeconds;
        var agePenalty = ageSeconds / 21_600.0;  // 21600s = 6 hours

        return Math.Round(engagementBoost - agePenalty, 7);
    }
}
