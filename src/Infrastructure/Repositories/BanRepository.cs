using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class BanRepository : IBanRepository
{
    private readonly ForumDbContext _dbContext;

    public BanRepository(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CommunityBan?> GetByIdAsync(BanId id, CancellationToken cancellationToken = default)
        => _dbContext.Bans
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(CommunityBan ban, CancellationToken cancellationToken = default)
    {
        await _dbContext.Bans.AddAsync(ban, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(BanId id, CancellationToken cancellationToken = default)
    {
        var ban = await _dbContext.Bans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (ban is null)
        {
            return;
        }

        _dbContext.Bans.Remove(ban);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}