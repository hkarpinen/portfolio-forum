using Forum.Application.Contracts;
using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class VoteManager : IVoteManager
{
    private readonly IVoteRepository _voteRepository;

    public VoteManager(IVoteRepository voteRepository)
    {
        _voteRepository = voteRepository;
    }

    public async Task<VoteResponse> CastAsync(CastVoteRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _voteRepository.GetByUserAndTargetAsync(
            new UserId(request.UserId), request.TargetType, request.TargetId, cancellationToken);

        if (existing is not null)
        {
            if (existing.Direction != request.Direction)
            {
                existing.SwitchDirection(request.Direction, DateTime.UtcNow);
                await _voteRepository.UpdateAsync(existing, cancellationToken);
            }
            return Map(existing);
        }

        var vote = Vote.Create(request.TargetType, request.TargetId, new UserId(request.UserId), request.Direction);

        await _voteRepository.AddAsync(vote, cancellationToken);
        return Map(vote);
    }

    public async Task<VoteResponse?> SwitchAsync(SwitchVoteRequest request, CancellationToken cancellationToken = default)
    {
        var vote = await _voteRepository.GetByIdAsync(new VoteId(request.VoteId), cancellationToken);

        if (vote is null)
        {
            return null;
        }

        vote.SwitchDirection(request.Direction, DateTime.UtcNow);
        await _voteRepository.UpdateAsync(vote, cancellationToken);
        return Map(vote);
    }

    public async Task<VoteResponse?> RetractAsync(RetractVoteRequest request, CancellationToken cancellationToken = default)
    {
        var vote = await _voteRepository.GetByIdAsync(new VoteId(request.VoteId), cancellationToken);

        if (vote is null)
        {
            return null;
        }

        vote.Retract(DateTime.UtcNow);
        await _voteRepository.RemoveAsync(vote.Id, cancellationToken);
        return Map(vote);
    }

    private static VoteResponse Map(Vote vote)
    {
        var upvotes = vote.Direction == VoteDirection.Upvote && vote.RetractedAt is null ? 1 : 0;
        var downvotes = vote.Direction == VoteDirection.Downvote && vote.RetractedAt is null ? 1 : 0;
        var calculatedScore = upvotes - downvotes;

        return new VoteResponse(
            vote.Id.Value,
            vote.TargetType,
            vote.TargetId,
            vote.UserId.Value,
            vote.Direction,
            vote.CastAt,
            vote.RetractedAt,
            calculatedScore);
    }
}
