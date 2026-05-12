using Forum.Application.Dtos;
using Forum.Domain.Aggregates;

namespace Forum.Application.Mappers;

public static class VoteMapper
{
    public static VoteDto ToDto(Vote vote) => new(
        vote.Id.Value,
        vote.TargetType,
        vote.TargetId,
        vote.UserId.Value,
        vote.Direction,
        vote.CastAt,
        (int)vote.Direction);
}
