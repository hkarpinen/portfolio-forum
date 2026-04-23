using Forum.Application.Contracts;
using Forum.Domain.Aggregates;
using Forum.Domain.Engines;
using Forum.Domain.ReadModels;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class CommentWorkflowManager : ICommentWorkflowManager
{
    private readonly ICommentRepository _commentRepository;
    private readonly ISpamDetectionEngine _spamDetectionEngine;
    private readonly IUserProjectionRepository _userProjectionRepository;

    public CommentWorkflowManager(
        ICommentRepository commentRepository,
        ISpamDetectionEngine spamDetectionEngine,
        IUserProjectionRepository userProjectionRepository)
    {
        _commentRepository = commentRepository;
        _spamDetectionEngine = spamDetectionEngine;
        _userProjectionRepository = userProjectionRepository;
    }

    public async Task<CommentResponse> CreateAsync(CreateCommentRequest request, CancellationToken cancellationToken = default)
    {
        if (_spamDetectionEngine.IsSpam(request.Content, request.AuthorId))
            throw new InvalidOperationException("Content was rejected as spam.");

        var comment = Comment.Create(
            new ThreadId(request.ThreadId),
            new UserId(request.AuthorId),
            request.Content);

        await _commentRepository.AddAsync(comment, cancellationToken);
        var proj = await _userProjectionRepository.GetByIdAsync(new UserId(request.AuthorId), cancellationToken);
        return Map(comment, request.ParentCommentId, proj);
    }

    public async Task<CommentResponse?> EditAsync(EditCommentRequest request, CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(new CommentId(request.CommentId), cancellationToken);
        if (comment is null) return null;

        if (_spamDetectionEngine.IsSpam(request.Content, comment.AuthorId.Value))
            throw new InvalidOperationException("Content was rejected as spam.");

        comment.Edit(request.Content, DateTime.UtcNow);
        await _commentRepository.UpdateAsync(comment, cancellationToken);
        var proj = await _userProjectionRepository.GetByIdAsync(comment.AuthorId, cancellationToken);
        return Map(comment, parentCommentId: null, proj);
    }

    public async Task<CommentResponse?> DeleteAsync(DeleteCommentRequest request, CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(new CommentId(request.CommentId), cancellationToken);
        if (comment is null) return null;

        comment.Delete(DateTime.UtcNow);
        await _commentRepository.UpdateAsync(comment, cancellationToken);
        var proj = await _userProjectionRepository.GetByIdAsync(comment.AuthorId, cancellationToken);
        return Map(comment, parentCommentId: null, proj);
    }

    private static CommentResponse Map(Comment comment, Guid? parentCommentId, UserProjection? proj)
        => new(
            comment.Id.Value,
            comment.ThreadId.Value,
            comment.AuthorId.Value,
            proj?.DisplayName ?? proj?.UserName,
            proj?.AvatarUrl,
            comment.Content,
            comment.CreatedAt,
            comment.EditedAt,
            comment.DeletedAt,
            parentCommentId);
}
