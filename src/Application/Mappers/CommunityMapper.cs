using Forum.Application.Contracts;
using Forum.Domain.Aggregates;
using Forum.Domain.ReadModels;

namespace Forum.Application.Mappers;

public static class CommunityMapper
{
    public static CommunityResponse ToResponse(
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
        activity,
        memberCount,
        threadCount,
        commentCount);
}
