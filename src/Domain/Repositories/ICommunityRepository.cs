using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Forum.Domain.Repositories;

public interface ICommunityRepository
{
    Task<Community?> GetByIdAsync(CommunityId id, CancellationToken cancellationToken = default);
    Task<Community?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Community community, CancellationToken cancellationToken = default);
    Task UpdateAsync(Community community, CancellationToken cancellationToken = default);
    Task DeleteAsync(CommunityId id, CancellationToken cancellationToken = default);
}
