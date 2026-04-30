using Forum.Application.Contracts;
using Forum.Application.Mappers;
using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class VoteManager : IVoteManager
{
    private readonly IVoteRepository _voteRepository;
    private readonly IThreadRepository _threadRepository;
    private readonly ICommentRepository _commentRepository;

    public VoteManager(
        IVoteRepository voteRepository,
        IThreadRepository threadRepository,
        ICommentRepository commentRepository)
    {
        _voteRepository = voteRepository;
        _threadRepository = threadRepository;
        _commentRepository = commentRepository;
    }

    public async Task<VoteResponse> CastAsync(CastVoteRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _voteRepository.GetByUserAndTargetAsync(
            new UserId(request.UserId), request.TargetType, request.TargetId, cancellationToken);

        if (existing is not null)
        {
            if (existing.Direction == request.Direction)
                return VoteMapper.ToResponse(existing);

            var delta = (int)request.Direction - (int)existing.Direction;
            existing.SwitchDirection(request.Direction, DateTime.UtcNow);
            await _voteRepository.UpdateAsync(existing, cancellationToken);
            await AdjustTargetScoreAsync(request.TargetType, request.TargetId, delta, cancellationToken);
            return VoteMapper.ToResponse(existing);
        }

        var vote = Vote.Create(request.TargetType, request.TargetId, new UserId(request.UserId), request.Direction);
        await _voteRepository.AddAsync(vote, cancellationToken);
        await AdjustTargetScoreAsync(request.TargetType, request.TargetId, (int)request.Direction, cancellationToken);
        return VoteMapper.ToResponse(vote);
    }

    public async Task<VoteResponse?> SwitchAsync(SwitchVoteRequest request, CancellationToken cancellationToken = default)
    {
        var vote = await _voteRepository.GetByIdAsync(new VoteId(request.VoteId), cancellationToken);
        if (vote is null)
            return null;

        var delta = (int)request.Direction - (int)vote.Direction;
        vote.SwitchDirection(request.Direction, DateTime.UtcNow);
        await _voteRepository.UpdateAsync(vote, cancellationToken);
        await AdjustTargetScoreAsync(vote.TargetType, vote.TargetId, delta, cancellationToken);
        return VoteMapper.ToResponse(vote);
    }

    public async Task<VoteResponse?> RetractAsync(RetractVoteRequest request, CancellationToken cancellationToken = default)
    {
        var vote = await _voteRepository.GetByIdAsync(new VoteId(request.VoteId), cancellationToken);
        if (vote is null)
            return null;

        var response = VoteMapper.ToResponse(vote);
        await _voteRepository.RemoveAsync(vote.Id, cancellationToken);
        await AdjustTargetScoreAsync(vote.TargetType, vote.TargetId, -(int)vote.Direction, cancellationToken);
        return response;
    }

    private async Task AdjustTargetScoreAsync(VoteTargetType targetType, Guid targetId, int delta, CancellationToken cancellationToken)
    {
        if (delta == 0) return;

        if (targetType == VoteTargetType.Thread)
        {
            var thread = await _threadRepository.GetByIdAsync(new ThreadId(targetId), cancellationToken);
            if (thread is not null)
            {
                thread.AdjustVoteScore(delta);
                await _threadRepository.UpdateAsync(thread, cancellationToken);
            }
        }
        else
        {
            var comment = await _commentRepository.GetByIdAsync(new CommentId(targetId), cancellationToken);
            if (comment is not null)
            {
                comment.AdjustVoteScore(delta);
                await _commentRepository.UpdateAsync(comment, cancellationToken);
            }
        }
    }

}

