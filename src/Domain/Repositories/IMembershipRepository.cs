using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;

namespace Forum.Domain.Repositories;

public interface IMembershipRepository
{
    Task<CommunityMembership?> GetByIdAsync(MembershipId id, CancellationToken cancellationToken = default);
    Task<CommunityMembership?> GetByUserAndCommunityAsync(UserId userId, CommunityId communityId, CancellationToken cancellationToken = default);
    Task AddAsync(CommunityMembership membership, CancellationToken cancellationToken = default);
    Task UpdateAsync(CommunityMembership membership, CancellationToken cancellationToken = default);
    Task DeleteAsync(MembershipId id, CancellationToken cancellationToken = default);
}
