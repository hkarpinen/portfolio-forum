using Forum.Domain.Aggregates;
using Forum.Application.Repositories;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class VoteRepository : IVoteRepository
{
    private readonly ForumDbContext _dbContext;

    public VoteRepository(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Vote?> GetByIdAsync(VoteId id, CancellationToken cancellationToken = default)
        => _dbContext.Votes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Vote?> GetByUserAndTargetAsync(UserId userId, VoteTargetType targetType, Guid targetId, CancellationToken cancellationToken = default)
        => _dbContext.Votes.FirstOrDefaultAsync(
            x => x.UserId == userId && x.TargetType == targetType && x.TargetId == targetId,
            cancellationToken);

    public async Task AddAsync(Vote vote, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Votes.FirstOrDefaultAsync(x => x.Id == vote.Id, cancellationToken);

        if (existing is null)
            await _dbContext.Votes.AddAsync(vote, cancellationToken);
        else
            _dbContext.Entry(existing).CurrentValues.SetValues(vote);
    }

    public Task UpdateAsync(Vote vote, CancellationToken cancellationToken = default)
    {
        _dbContext.Votes.Update(vote);
        return Task.CompletedTask;
    }

    public async Task RemoveAsync(VoteId id, CancellationToken cancellationToken = default)
    {
        var vote = await _dbContext.Votes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (vote is null) return;
        _dbContext.Votes.Remove(vote);
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
