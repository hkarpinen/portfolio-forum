using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class ModerationLogRepository : IModerationLogRepository
{
    private readonly ForumDbContext _dbContext;

    public ModerationLogRepository(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ModerationLog?> GetByIdAsync(LogId id, CancellationToken cancellationToken = default)
        => _dbContext.ModerationLogs
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(ModerationLog log, CancellationToken cancellationToken = default)
    {
        await _dbContext.ModerationLogs.AddAsync(log, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}