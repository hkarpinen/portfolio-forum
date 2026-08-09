using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Repositories;

public interface IBanRepository
{
    Task<CommunityBan?> GetByIdAsync(BanId id, CancellationToken cancellationToken = default);

    /// <summary>Consulted by the posting paths — a ban blocks FUTURE content only.</summary>
    Task<bool> IsBannedAsync(CommunityId communityId, UserId userId, CancellationToken cancellationToken = default);
    Task AddAsync(CommunityBan ban, CancellationToken cancellationToken = default);
    Task RemoveAsync(CommunityBan ban, CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
}
