using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Managers;

public interface IMembershipManager
{
    Task<MembershipDto> JoinAsync(JoinCommunityCommand command, CancellationToken cancellationToken = default);
    Task<MembershipDto> InviteAsync(InviteMemberCommand command, CancellationToken cancellationToken = default);
    Task<MembershipDto?> LeaveAsync(LeaveCommunityCommand command, CancellationToken cancellationToken = default);
    Task<MembershipDto?> AppointModeratorAsync(AppointModeratorCommand command, CancellationToken cancellationToken = default);
    Task<MembershipDto?> RemoveModeratorAsync(RemoveModeratorCommand command, CancellationToken cancellationToken = default);
}
