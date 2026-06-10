namespace Forum.Domain.ValueObjects;

/// <summary>
/// Where a `ForumThread` sits in its authoring lifecycle. A thread spends
/// time as a <see cref="Draft"/> while the author composes it, then
/// transitions to <see cref="Published"/> on publication — which is the
/// moment public listings and the `ThreadCreated` domain event surface.
/// </summary>
public enum ThreadStatus
{
    /// <summary>Author is still composing — visible only to the author.</summary>
    Draft = 0,
    /// <summary>Visible in community feeds, search, and the hot ranking.</summary>
    Published = 1
}
