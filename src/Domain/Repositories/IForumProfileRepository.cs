using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Forum.Domain.Repositories;

public interface IForumProfileRepository
{
    Task<ForumProfile?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task AddAsync(ForumProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(ForumProfile profile, CancellationToken cancellationToken = default);
}
