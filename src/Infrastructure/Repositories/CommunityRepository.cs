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

    public Task<Community?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => _dbContext.Communities
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
        => _dbContext.Communities
            .AnyAsync(x => x.Slug == slug, cancellationToken);

    public async Task AddAsync(Community community, CancellationToken cancellationToken = default)
    {
        await _dbContext.Communities.AddAsync(community, cancellationToken);
        foreach (var e in community.DomainEvents) _dbContext.AddToOutbox(e);
        community.ClearDomainEvents();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Community community, CancellationToken cancellationToken = default)
    {
        _dbContext.Communities.Update(community);
        foreach (var e in community.DomainEvents) _dbContext.AddToOutbox(e);
        community.ClearDomainEvents();
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