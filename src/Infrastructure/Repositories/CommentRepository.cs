using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

internal sealed class CommentRepository : ICommentRepository
{
    private readonly ForumDbContext _dbContext;

    public CommentRepository(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Comment?> GetByIdAsync(CommentId id, CancellationToken cancellationToken = default)
        => _dbContext.Comments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Comments.AddAsync(comment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        _dbContext.Comments.Update(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(CommentId id, CancellationToken cancellationToken = default)
    {
        var comment = await _dbContext.Comments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (comment is null)
        {
            return;
        }

        _dbContext.Comments.Remove(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}