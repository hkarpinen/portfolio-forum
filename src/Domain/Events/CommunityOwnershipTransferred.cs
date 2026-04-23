using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record CommunityOwnershipTransferred(
    CommunityId CommunityId,
    UserId PreviousOwnerId,
    UserId NewOwnerId,
    DateTime TransferredAt
);
