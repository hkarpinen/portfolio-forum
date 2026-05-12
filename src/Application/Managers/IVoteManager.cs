using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Managers;

public interface IVoteManager
{
    Task<VoteDto> CastAsync(CastVoteCommand command, CancellationToken cancellationToken = default);
    Task<VoteDto?> SwitchAsync(SwitchVoteCommand command, CancellationToken cancellationToken = default);
    Task<VoteDto?> RetractAsync(RetractVoteCommand command, CancellationToken cancellationToken = default);
}
