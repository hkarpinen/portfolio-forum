using Forum.Domain.ValueObjects;

namespace Forum.Application.Dtos;

public sealed record VoteDto(
    Guid VoteId,
    VoteTargetType TargetType,
    Guid TargetId,
    Guid UserId,
    VoteDirection Direction,
    DateTime CastAt,
    int CalculatedScore);
