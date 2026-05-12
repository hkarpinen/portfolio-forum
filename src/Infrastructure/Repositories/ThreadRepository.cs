using Forum.Domain.Aggregates;
using Forum.Application.Repositories;
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
        => _dbContext.Threads.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(ForumThread thread, CancellationToken cancellationToken = default)
    {
        await _dbContext.Threads.AddAsync(thread, cancellationToken);
    }

    public Task UpdateAsync(ForumThread thread, CancellationToken cancellationToken = default)
    {
        _dbContext.Threads.Update(thread);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(ThreadId id, CancellationToken cancellationToken = default)
    {
        var thread = await _dbContext.Threads.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (thread is null) return;
        _dbContext.Threads.Remove(thread);
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
