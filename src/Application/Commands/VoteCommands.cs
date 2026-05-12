using Forum.Domain.ValueObjects;

namespace Forum.Application.Commands;

public sealed record CastVoteCommand(VoteTargetType TargetType, Guid TargetId, VoteDirection Direction, Guid UserId = default);
public sealed record SwitchVoteCommand(VoteDirection Direction, Guid VoteId = default);
public sealed record RetractVoteCommand(Guid VoteId = default);
