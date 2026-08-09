using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Mappers;
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
    private readonly IBanRepository _banRepository;

    public CommentWorkflowManager(
        ICommentRepository commentRepository,
        IThreadRepository threadRepository,
        ICommunityRepository communityRepository,
        IBanRepository banRepository)
    {
        _commentRepository = commentRepository;
        _threadRepository = threadRepository;
        _communityRepository = communityRepository;
        _banRepository = banRepository;
    }

    public async Task<Guid> CreateAsync(CreateCommentCommand command, CancellationToken cancellationToken = default)
    {
        if (SpamDetectionEngine.IsSpam(command.Content, command.AuthorId))
            throw new InvalidOperationException("Content was rejected as spam.");

        // Banned from the community the thread sits in — see the same check on
        // thread creation. Reading the thread is still allowed; posting isn't.
        var host = await _threadRepository.GetByIdAsync(new ThreadId(command.ThreadId), cancellationToken);
        if (host is not null && await _banRepository.IsBannedAsync(
                host.CommunityId, new UserId(command.AuthorId), cancellationToken))
            throw new UnauthorizedAccessException("You are banned from this community.");

        var comment = Comment.Create(
            new ThreadId(command.ThreadId),
            new UserId(command.AuthorId),
            command.Content,
            command.ParentCommentId.HasValue ? new CommentId(command.ParentCommentId.Value) : null);

        await _commentRepository.AddAsync(comment, cancellationToken);
        await _commentRepository.CommitAsync(cancellationToken);
        return comment.Id.Value;
    }

    public async Task<CommentDto?> EditAsync(EditCommentCommand command, CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(new CommentId(command.CommentId), cancellationToken);
        if (comment is null) return null;

        if (SpamDetectionEngine.IsSpam(command.Content, comment.AuthorId.Value))
            throw new InvalidOperationException("Content was rejected as spam.");

        comment.Edit(command.Content, DateTime.UtcNow);
        await _commentRepository.UpdateAsync(comment, cancellationToken);
        await _commentRepository.CommitAsync(cancellationToken);

        // Frontend expects the updated comment (including new EditedAt) so it can patch its cache.
        // Vote score isn't recomputed here; the caller still has the current value client-side.
        return CommentMapper.ToDto(comment, authorName: null, authorAvatarUrl: null, voteScore: 0);
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
