using Forum.Domain.Aggregates;
using Forum.Domain.ReadModels;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class UserProjectionRepository : IUserProjectionRepository
{
    private readonly ForumDbContext _dbContext;

    public UserProjectionRepository(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserProjection?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        => _dbContext.UserProjections
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddOrUpdateAsync(UserProjection projection, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserProjections
            .FirstOrDefaultAsync(x => x.Id == projection.Id, cancellationToken);

        if (existing is null)
        {
            await _dbContext.UserProjections.AddAsync(projection, cancellationToken);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(projection);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}