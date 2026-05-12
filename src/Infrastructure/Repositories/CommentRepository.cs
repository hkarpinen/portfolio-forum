using Forum.Domain.Aggregates;
using Forum.Application.Repositories;
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
        => _dbContext.Comments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Comments.AddAsync(comment, cancellationToken);
    }

    public Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        _dbContext.Comments.Update(comment);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(CommentId id, CancellationToken cancellationToken = default)
    {
        var comment = await _dbContext.Comments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (comment is null) return;
        _dbContext.Comments.Remove(comment);
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
