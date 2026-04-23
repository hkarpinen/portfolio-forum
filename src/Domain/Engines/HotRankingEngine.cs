using System;

namespace Forum.Domain.Engines;

internal sealed class HotRankingEngine : IHotRankingEngine
{
    public double CalculateHotScore(DateTime createdAt, int score, int commentCount)
    {
        // Example Reddit-style hot ranking formula
        var order = Math.Log10(Math.Max(Math.Abs(score), 1));
        var seconds = (createdAt - new DateTime(1970, 1, 1)).TotalSeconds - 1134028003;
        return Math.Round(order + seconds / 45000 + commentCount * 0.1, 7);
    }
}
