using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record UserUnbanned(
    BanId BanId,
    CommunityId CommunityId,
    UserId UserId,
    DateTime UnbannedAt
);
