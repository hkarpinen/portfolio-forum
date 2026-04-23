using Forum.Domain.ValueObjects;

namespace Forum.Application.Contracts;

public sealed record CastVoteRequest(VoteTargetType TargetType, Guid TargetId, Guid UserId, VoteDirection Direction);
public sealed record SwitchVoteRequest(Guid VoteId, VoteDirection Direction);
public sealed record RetractVoteRequest(Guid VoteId);

public sealed record VoteResponse(
    Guid VoteId,
    VoteTargetType TargetType,
    Guid TargetId,
    Guid UserId,
    VoteDirection Direction,
    DateTime CastAt,
    DateTime? RetractedAt,
    int CalculatedScore);
