using System.Text.Json.Serialization;

namespace Forum.Application.Dtos;

/// <summary>
/// Base type for polymorphic search results. The wire JSON carries an `itemType` discriminator
/// chosen by <see cref="JsonPolymorphicAttribute"/>, so the frontend can switch on it instead of
/// inspecting optional fields.
/// </summary>
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
