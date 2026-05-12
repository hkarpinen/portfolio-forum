using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Mappers;
using Forum.Domain.Aggregates;
using Forum.Application.Repositories;
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

    public async Task<VoteDto> CastAsync(CastVoteCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await _voteRepository.GetByUserAndTargetAsync(
            new UserId(command.UserId), command.TargetType, command.TargetId, cancellationToken);

        if (existing is not null)
        {
            if (existing.Direction == command.Direction)
                return VoteMapper.ToDto(existing);

            var delta = (int)command.Direction - (int)existing.Direction;
            existing.SwitchDirection(command.Direction, DateTime.UtcNow);
            await _voteRepository.UpdateAsync(existing, cancellationToken);
            await AdjustTargetScoreAsync(command.TargetType, command.TargetId, delta, cancellationToken);
            await _voteRepository.CommitAsync(cancellationToken);
            return VoteMapper.ToDto(existing);
        }

        var vote = Vote.Create(command.TargetType, command.TargetId, new UserId(command.UserId), command.Direction);
        await _voteRepository.AddAsync(vote, cancellationToken);
        await AdjustTargetScoreAsync(command.TargetType, command.TargetId, (int)command.Direction, cancellationToken);
        await _voteRepository.CommitAsync(cancellationToken);
        return VoteMapper.ToDto(vote);
    }

    public async Task<VoteDto?> SwitchAsync(SwitchVoteCommand command, CancellationToken cancellationToken = default)
    {
        var vote = await _voteRepository.GetByIdAsync(new VoteId(command.VoteId), cancellationToken);
        if (vote is null)
            return null;

        var delta = (int)command.Direction - (int)vote.Direction;
        vote.SwitchDirection(command.Direction, DateTime.UtcNow);
        await _voteRepository.UpdateAsync(vote, cancellationToken);
        await AdjustTargetScoreAsync(vote.TargetType, vote.TargetId, delta, cancellationToken);
        await _voteRepository.CommitAsync(cancellationToken);
        return VoteMapper.ToDto(vote);
    }

    public async Task<VoteDto?> RetractAsync(RetractVoteCommand command, CancellationToken cancellationToken = default)
    {
        var vote = await _voteRepository.GetByIdAsync(new VoteId(command.VoteId), cancellationToken);
        if (vote is null)
            return null;

        var dto = VoteMapper.ToDto(vote);
        await _voteRepository.RemoveAsync(vote.Id, cancellationToken);
        await AdjustTargetScoreAsync(vote.TargetType, vote.TargetId, -(int)vote.Direction, cancellationToken);
        await _voteRepository.CommitAsync(cancellationToken);
        return dto;
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
