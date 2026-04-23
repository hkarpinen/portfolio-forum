using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Forum.Domain.Repositories;

public interface IThreadRepository
{
    Task<ForumThread?> GetByIdAsync(ThreadId id, CancellationToken cancellationToken = default);
    Task AddAsync(ForumThread thread, CancellationToken cancellationToken = default);
    Task UpdateAsync(ForumThread thread, CancellationToken cancellationToken = default);
    Task DeleteAsync(ThreadId id, CancellationToken cancellationToken = default);
}
