using Forum.Application.Contracts;
using Forum.Domain.Aggregates;
using Forum.Domain.Engines;
using Forum.Domain.ReadModels;

namespace Forum.Application;

/// <summary>
/// Single place that constructs a <see cref="ThreadResponse"/> from a domain
/// aggregate and supporting data.  Both <c>ThreadWorkflowManager</c> (command
/// side) and <c>ThreadQuery</c> (read side) delegate here so the mapping logic
/// is defined exactly once.
/// </summary>
/// 
/// TODO: Should refactor this class later. Not really a factory.
internal static class ThreadResponseFactory
{
    public static ThreadResponse From(
        ForumThread thread,
        int score,
        int commentCount,
        string? authorDisplayName,
        string? authorAvatarUrl,
        IHotRankingEngine hotRankingEngine)
    {
        var hotScore = hotRankingEngine.CalculateHotScore(thread.CreatedAt, score, commentCount);
        return new ThreadResponse(
            thread.Id.Value,
            thread.CommunityId.Value,
            thread.AuthorId.Value,
            authorDisplayName,
            authorAvatarUrl,
            thread.Title,
            thread.Content,
            thread.CreatedAt,
            thread.EditedAt,
            thread.IsLocked,
            thread.IsPinned,
            thread.DeletedAt,
            hotScore,
            score);
    }

    /// <summary>Convenience overload that resolves display name from a <see cref="UserProjection"/>.</summary>
    public static ThreadResponse From(
        ForumThread thread,
        int score,
        int commentCount,
        UserProjection? proj,
        IHotRankingEngine hotRankingEngine) =>
        From(thread, score, commentCount,
            proj?.EffectiveName,
            proj?.AvatarUrl,
            hotRankingEngine);

    /// <summary>Slim acknowledgment returned by every command mutation.</summary>
    public static ThreadMutationResponse ToMutation(ForumThread thread) =>
        new(
            thread.Id.Value,
            thread.IsLocked,
            thread.IsPinned,
            thread.EditedAt,
            thread.DeletedAt);

    /// <summary>List-row projection — no <c>Content</c> field.</summary>
    public static ThreadSummaryResponse ToSummary(
        ForumThread thread,
        int score,
        int commentCount,
        string? authorDisplayName,
        string? authorAvatarUrl,
        IHotRankingEngine hotRankingEngine)
    {
        var hotScore = hotRankingEngine.CalculateHotScore(thread.CreatedAt, score, commentCount);
        return new ThreadSummaryResponse(
            thread.Id.Value,
            thread.CommunityId.Value,
            thread.AuthorId.Value,
            authorDisplayName,
            authorAvatarUrl,
            thread.Title,
            thread.CreatedAt,
            hotScore,
            score);
    }

    /// <summary>Convenience overload that resolves display name from a <see cref="UserProjection"/>.</summary>
    public static ThreadSummaryResponse ToSummary(
        ForumThread thread,
        int score,
        int commentCount,
        UserProjection? proj,
        IHotRankingEngine hotRankingEngine) =>
        ToSummary(thread, score, commentCount,
            proj?.EffectiveName,
            proj?.AvatarUrl,
            hotRankingEngine);
}
