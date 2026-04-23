using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class CommunityRepository : ICommunityRepository
{
    private readonly ForumDbContext _dbContext;

    public CommunityRepository(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Community?> GetByIdAsync(CommunityId id, CancellationToken cancellationToken = default)
        => _dbContext.Communities
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Community?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => _dbContext.Communities
            .FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);

    public async Task AddAsync(Community community, CancellationToken cancellationToken = default)
    {
        await _dbContext.Communities.AddAsync(community, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Community community, CancellationToken cancellationToken = default)
    {
        _dbContext.Communities.Update(community);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(CommunityId id, CancellationToken cancellationToken = default)
    {
        var community = await _dbContext.Communities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (community is null)
        {
            return;
        }

        _dbContext.Communities.Remove(community);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}