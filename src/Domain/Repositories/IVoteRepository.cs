using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Forum.Domain.Repositories;

public interface IVoteRepository
{
    Task<Vote?> GetByIdAsync(VoteId id, CancellationToken cancellationToken = default);
    Task<Vote?> GetByUserAndTargetAsync(UserId userId, VoteTargetType targetType, Guid targetId, CancellationToken cancellationToken = default);
    Task AddAsync(Vote vote, CancellationToken cancellationToken = default);
    Task UpdateAsync(Vote vote, CancellationToken cancellationToken = default);
    Task RemoveAsync(VoteId id, CancellationToken cancellationToken = default);
}
