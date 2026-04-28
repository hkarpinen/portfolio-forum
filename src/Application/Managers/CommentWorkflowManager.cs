using Forum.Application.Contracts;
using Forum.Domain.Aggregates;
using Forum.Domain.Engines;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class CommentWorkflowManager : ICommentWorkflowManager
{
    private readonly ICommentRepository _commentRepository;
    private readonly IThreadRepository _threadRepository;
    private readonly ICommunityRepository _communityRepository;
    private readonly ISpamDetectionEngine _spamDetectionEngine;

    public CommentWorkflowManager(
        ICommentRepository commentRepository,
        IThreadRepository threadRepository,
        ICommunityRepository communityRepository,
        ISpamDetectionEngine spamDetectionEngine)
    {
        _commentRepository = commentRepository;
        _threadRepository = threadRepository;
        _communityRepository = communityRepository;
        _spamDetectionEngine = spamDetectionEngine;
    }

    public async Task<Guid> CreateAsync(CreateCommentRequest request, CancellationToken cancellationToken = default)
    {
        if (_spamDetectionEngine.IsSpam(request.Content, request.AuthorId))
            throw new InvalidOperationException("Content was rejected as spam.");

        var comment = Comment.Create(
            new ThreadId(request.ThreadId),
            new UserId(request.AuthorId),
            request.Content,
            request.ParentCommentId.HasValue ? new CommentId(request.ParentCommentId.Value) : null);

        await _commentRepository.AddAsync(comment, cancellationToken);
        return comment.Id.Value;
    }

    public async Task<bool> EditAsync(EditCommentRequest request, CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(new CommentId(request.CommentId), cancellationToken);
        if (comment is null) return false;

        if (_spamDetectionEngine.IsSpam(request.Content, comment.AuthorId.Value))
            throw new InvalidOperationException("Content was rejected as spam.");

        comment.Edit(request.Content, DateTime.UtcNow);
        await _commentRepository.UpdateAsync(comment, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(DeleteCommentRequest request, CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(new CommentId(request.CommentId), cancellationToken);
        if (comment is null) return false;

        comment.Delete(DateTime.UtcNow);
        await _commentRepository.UpdateAsync(comment, cancellationToken);
        return true;
    }
}
