using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Repositories;

public interface ICommunityRepository
{
    Task<Community?> GetByIdAsync(CommunityId id, CancellationToken cancellationToken = default);
    Task<Community?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
    Task AddAsync(Community community, CancellationToken cancellationToken = default);
    Task UpdateAsync(Community community, CancellationToken cancellationToken = default);
    Task DeleteAsync(CommunityId id, CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
}
