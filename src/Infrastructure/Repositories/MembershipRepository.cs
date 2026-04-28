using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class MembershipRepository : IMembershipRepository
{
    private readonly ForumDbContext _dbContext;

    public MembershipRepository(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CommunityMembership?> GetByIdAsync(MembershipId id, CancellationToken cancellationToken = default)
        => _dbContext.Memberships
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<CommunityMembership?> GetByUserAndCommunityAsync(UserId userId, CommunityId communityId, CancellationToken cancellationToken = default)
        => _dbContext.Memberships
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CommunityId == communityId, cancellationToken);

    public async Task AddAsync(CommunityMembership membership, CancellationToken cancellationToken = default)
    {
        await _dbContext.Memberships.AddAsync(membership, cancellationToken);
        foreach (var e in membership.DomainEvents) _dbContext.AddToOutbox(e);
        membership.ClearDomainEvents();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CommunityMembership membership, CancellationToken cancellationToken = default)
    {
        _dbContext.Memberships.Update(membership);
        foreach (var e in membership.DomainEvents) _dbContext.AddToOutbox(e);
        membership.ClearDomainEvents();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(MembershipId id, CancellationToken cancellationToken = default)
    {
        var membership = await _dbContext.Memberships.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (membership is null)
        {
            return;
        }

        _dbContext.Memberships.Remove(membership);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}