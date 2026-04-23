using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Forum.Domain.Repositories;

public interface IModerationLogRepository
{
    Task<ModerationLog?> GetByIdAsync(LogId id, CancellationToken cancellationToken = default);
    Task AddAsync(ModerationLog log, CancellationToken cancellationToken = default);
}
