using Forum.Application.Dtos;
using Forum.Domain.Aggregates;

namespace Forum.Application.Mappers;

public static class CommunityMapper
{
    public static CommunityDto ToDto(
        Community c,
        CommunityActivitySnapshot? activity = null,
        int memberCount = 0,
        int threadCount = 0,
        int commentCount = 0) => new(
        c.Id.Value,
        c.Slug,
        c.Name,
        c.Description,
        c.ImageUrl,
        c.Visibility,
        c.OwnerId.Value,
        c.CreatedAt,
        c.UpdatedAt,
        c.Rules,
        activity,
        memberCount,
        threadCount,
        commentCount);
}
