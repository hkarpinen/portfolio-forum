namespace Forum.Application.Dtos;

public sealed record SearchResultItem(
    string ItemType,
    Guid ItemId,
    string Title,
    string? Snippet,
    Guid CommunityId,
    DateTime CreatedAt,
    double RankScore);

public sealed record SearchDto(IReadOnlyCollection<SearchResultItem> Items, int TotalCount);
