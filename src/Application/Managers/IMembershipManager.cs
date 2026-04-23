using Forum.Application.Contracts;

namespace Forum.Application.Managers;

public interface IMembershipManager
{
    Task<MembershipResponse> JoinAsync(JoinCommunityRequest request, CancellationToken cancellationToken = default);
    Task<MembershipResponse> InviteAsync(InviteMemberRequest request, CancellationToken cancellationToken = default);
    Task<MembershipResponse?> LeaveAsync(LeaveCommunityRequest request, CancellationToken cancellationToken = default);
    Task<MembershipResponse?> AppointModeratorAsync(AppointModeratorRequest request, CancellationToken cancellationToken = default);
    Task<MembershipResponse?> RemoveModeratorAsync(RemoveModeratorRequest request, CancellationToken cancellationToken = default);
}
