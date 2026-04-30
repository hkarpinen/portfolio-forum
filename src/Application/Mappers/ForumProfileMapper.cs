using Forum.Application.Contracts;
using Forum.Domain.Aggregates;
using Forum.Domain.ReadModels;

namespace Forum.Application.Mappers;

public static class ForumProfileMapper
{
    public static ForumProfileResponse ToResponse(ForumProfile profile, UserProjection? proj = null) => new(
        profile.UserId.Value,
        proj?.EffectiveName,
        proj?.AvatarUrl,
        profile.Bio,
        profile.Signature,
        profile.CreatedAt,
        profile.UpdatedAt);
}
