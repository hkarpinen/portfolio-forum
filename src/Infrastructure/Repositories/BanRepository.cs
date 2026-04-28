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
        foreach (var e in ban.DomainEvents) _dbContext.AddToOutbox(e);
        ban.ClearDomainEvents();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(CommunityBan ban, CancellationToken cancellationToken = default)
    {
        foreach (var e in ban.DomainEvents) _dbContext.AddToOutbox(e);
        ban.ClearDomainEvents();
        _dbContext.Bans.Remove(ban);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}