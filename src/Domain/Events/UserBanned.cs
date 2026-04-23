using Forum.Domain.ValueObjects;

namespace Forum.Domain.Events;

public sealed record UserBanned(
    BanId BanId,
    CommunityId CommunityId,
    UserId UserId,
    DateTime BannedAt,
    string? Reason
);
