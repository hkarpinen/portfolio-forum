using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Repositories;

public interface IBanRepository
{
    Task<CommunityBan?> GetByIdAsync(BanId id, CancellationToken cancellationToken = default);
    Task AddAsync(CommunityBan ban, CancellationToken cancellationToken = default);
    Task RemoveAsync(CommunityBan ban, CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
}
