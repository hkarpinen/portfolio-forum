using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class ThreadRepository : IThreadRepository
{
    private readonly ForumDbContext _dbContext;

    public ThreadRepository(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ForumThread?> GetByIdAsync(ThreadId id, CancellationToken cancellationToken = default)
        => _dbContext.Threads
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(ForumThread thread, CancellationToken cancellationToken = default)
    {
        await _dbContext.Threads.AddAsync(thread, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ForumThread thread, CancellationToken cancellationToken = default)
    {
        _dbContext.Threads.Update(thread);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ThreadId id, CancellationToken cancellationToken = default)
    {
        var thread = await _dbContext.Threads.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (thread is null)
        {
            return;
        }

        _dbContext.Threads.Remove(thread);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}