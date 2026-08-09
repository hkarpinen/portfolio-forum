using System.Text.Json.Serialization;

namespace Forum.Application.Dtos;

/// <summary>The wire carries an `itemType` discriminator, so callers switch on it
/// rather than probing optional fields.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "itemType")]
[JsonDerivedType(typeof(ThreadSearchResultDto), "thread")]
[JsonDerivedType(typeof(CommunitySearchResultDto), "community")]
public abstract record SearchResultDto(Guid ItemId, DateTime CreatedAt, double RankScore);

public sealed record ThreadSearchResultDto(
    Guid ItemId,
    string Title,
    string? Snippet,
    Guid CommunityId,
    string? CommunitySlug,
    string? CommunityName,
    DateTime CreatedAt,
    double RankScore)
    : SearchResultDto(ItemId, CreatedAt, RankScore);

public sealed record CommunitySearchResultDto(
    Guid ItemId,
    string Name,
    string? Description,
    string Slug,
    DateTime CreatedAt,
    double RankScore)
    : SearchResultDto(ItemId, CreatedAt, RankScore);

public sealed record SearchDto(IReadOnlyCollection<SearchResultDto> Items, int TotalCount);
