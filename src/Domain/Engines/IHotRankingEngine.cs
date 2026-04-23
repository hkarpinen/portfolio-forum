namespace Forum.Domain.Engines;

public interface IHotRankingEngine
{
    double CalculateHotScore(DateTime createdAt, int score, int commentCount);
}
