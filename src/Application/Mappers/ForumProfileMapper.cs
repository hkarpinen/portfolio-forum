using Forum.Application.Dtos;
using Forum.Domain.Aggregates;

namespace Forum.Application.Mappers;

public static class ForumProfileMapper
{
    public static ForumProfileDto ToDto(ForumProfile profile) => new(
        profile.UserId.Value,
        null,
        null,
        profile.Bio,
        profile.Signature,
        profile.CreatedAt,
        profile.UpdatedAt);
}
