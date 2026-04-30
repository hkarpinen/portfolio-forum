using Forum.Application.Contracts;
using Forum.Domain.Aggregates;

namespace Forum.Application.Mappers;

public static class VoteMapper
{
    public static VoteResponse ToResponse(Vote vote) => new(
        vote.Id.Value,
        vote.TargetType,
        vote.TargetId,
        vote.UserId.Value,
        vote.Direction,
        vote.CastAt,
        (int)vote.Direction);
}
