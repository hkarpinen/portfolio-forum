using Forum.Domain.Aggregates;
using Forum.Domain.ReadModels;
using Forum.Domain.ValueObjects;

namespace Forum.Domain.Repositories;

public interface IUserProjectionRepository
{
    Task<UserProjection?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(UserProjection projection, CancellationToken cancellationToken = default);
}
