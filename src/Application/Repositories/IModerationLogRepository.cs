using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Repositories;

public interface IModerationLogRepository
{
    Task<ModerationLog?> GetByIdAsync(LogId id, CancellationToken cancellationToken = default);
    Task AddAsync(ModerationLog log, CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
}
