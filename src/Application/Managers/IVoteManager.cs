using Forum.Application.Contracts;

namespace Forum.Application.Managers;

public interface IVoteManager
{
    Task<VoteResponse> CastAsync(CastVoteRequest request, CancellationToken cancellationToken = default);
    Task<VoteResponse?> SwitchAsync(SwitchVoteRequest request, CancellationToken cancellationToken = default);
    Task<VoteResponse?> RetractAsync(RetractVoteRequest request, CancellationToken cancellationToken = default);
}
