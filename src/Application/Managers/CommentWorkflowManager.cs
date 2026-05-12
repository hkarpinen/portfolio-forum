using Forum.Application.Commands;
using Forum.Domain.Aggregates;
using Forum.Domain.Engines;
using Forum.Application.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class CommentWorkflowManager : ICommentWorkflowManager
{
    private readonly ICommentRepository _commentRepository;
    private readonly IThreadRepository _threadRepository;
    private readonly ICommunityRepository _communityRepository;

    public CommentWorkflowManager(
        ICommentRepository commentRepository,
        IThreadRepository threadRepository,
        ICommunityRepository communityRepository)
    {
        _commentRepository = commentRepository;
        _threadRepository = threadRepository;
        _communityRepository = communityRepository;
    }

    public async Task<Guid> CreateAsync(CreateCommentCommand command, CancellationToken cancellationToken = default)
    {
        if (SpamDetectionEngine.IsSpam(command.Content, command.AuthorId))
            throw new InvalidOperationException("Content was rejected as spam.");

        var comment = Comment.Create(
            new ThreadId(command.ThreadId),
            new UserId(command.AuthorId),
            command.Content,
            command.ParentCommentId.HasValue ? new CommentId(command.ParentCommentId.Value) : null);

        await _commentRepository.AddAsync(comment, cancellationToken);
        await _commentRepository.CommitAsync(cancellationToken);
        return comment.Id.Value;
    }

    public async Task<bool> EditAsync(EditCommentCommand command, CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(new CommentId(command.CommentId), cancellationToken);
        if (comment is null) return false;

        if (SpamDetectionEngine.IsSpam(command.Content, comment.AuthorId.Value))
            throw new InvalidOperationException("Content was rejected as spam.");

        comment.Edit(command.Content, DateTime.UtcNow);
        await _commentRepository.UpdateAsync(comment, cancellationToken);
        await _commentRepository.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(DeleteCommentCommand command, CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(new CommentId(command.CommentId), cancellationToken);
        if (comment is null) return false;

        comment.Delete(DateTime.UtcNow);
        await _commentRepository.UpdateAsync(comment, cancellationToken);
        await _commentRepository.CommitAsync(cancellationToken);
        return true;
    }
}
