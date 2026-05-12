using Forum.Domain.Aggregates;
using Forum.Application.Repositories;
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
        => _dbContext.Bans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(CommunityBan ban, CancellationToken cancellationToken = default)
    {
        await _dbContext.Bans.AddAsync(ban, cancellationToken);
    }

    public Task RemoveAsync(CommunityBan ban, CancellationToken cancellationToken = default)
    {
        _dbContext.Bans.Remove(ban);
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
