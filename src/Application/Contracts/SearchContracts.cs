namespace Forum.Application.Contracts;

public enum SearchScope
{
    All,
    Communities,
    Threads,
    Comments
}

public enum SearchSort
{
    Relevance,
    Newest,
    Hot,
    Top
}

public sealed record SearchQueryRequest(string Query, SearchScope Scope, SearchSort Sort, int Page = 1, int PageSize = 20);

public sealed record SearchResultItem(
    string ItemType,
    Guid ItemId,
    string Title,
    string? Snippet,
    Guid CommunityId,
    DateTime CreatedAt,
    double RankScore);

public sealed record SearchResponse(IReadOnlyCollection<SearchResultItem> Items, int TotalCount);
