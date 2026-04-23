using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class ForumProfileRepository : IForumProfileRepository
{
    private readonly ForumDbContext _dbContext;

    public ForumProfileRepository(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ForumProfile?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
        => _dbContext.ForumProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task AddAsync(ForumProfile profile, CancellationToken cancellationToken = default)
    {
        await _dbContext.ForumProfiles.AddAsync(profile, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        profile.ClearDomainEvents();
    }

    public async Task UpdateAsync(ForumProfile profile, CancellationToken cancellationToken = default)
    {
        _dbContext.ForumProfiles.Update(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
        profile.ClearDomainEvents();
    }
}
