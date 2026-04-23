using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
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
        => _dbContext.Votes
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Vote?> GetByUserAndTargetAsync(UserId userId, VoteTargetType targetType, Guid targetId, CancellationToken cancellationToken = default)
        => _dbContext.Votes
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.TargetType == targetType && x.TargetId == targetId && x.RetractedAt == null,
                cancellationToken);

    public async Task AddAsync(Vote vote, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Votes.FirstOrDefaultAsync(x => x.Id == vote.Id, cancellationToken);

        if (existing is null)
        {
            await _dbContext.Votes.AddAsync(vote, cancellationToken);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(vote);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Vote vote, CancellationToken cancellationToken = default)
    {
        _dbContext.Votes.Update(vote);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(VoteId id, CancellationToken cancellationToken = default)
    {
        var vote = await _dbContext.Votes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (vote is null)
        {
            return;
        }

        _dbContext.Votes.Remove(vote);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}