using Forum.Application.Contracts;
using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class MembershipManager : IMembershipManager
{
    private readonly IMembershipRepository _membershipRepository;

    public MembershipManager(IMembershipRepository membershipRepository)
    {
        _membershipRepository = membershipRepository;
    }

    public async Task<MembershipResponse> JoinAsync(JoinCommunityRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _membershipRepository.GetByUserAndCommunityAsync(
            new UserId(request.UserId), new CommunityId(request.CommunityId), cancellationToken);

        if (existing is not null)
            return Map(existing, isInvite: false);

        var membership = CommunityMembership.Create(
            new CommunityId(request.CommunityId),
            new UserId(request.UserId));

        await _membershipRepository.AddAsync(membership, cancellationToken);
        return Map(membership, isInvite: false);
    }

    public async Task<MembershipResponse> InviteAsync(InviteMemberRequest request, CancellationToken cancellationToken = default)
    {
        var membership = CommunityMembership.Create(
            new CommunityId(request.CommunityId),
            new UserId(request.UserId));

        await _membershipRepository.AddAsync(membership, cancellationToken);
        return Map(membership, isInvite: true);
    }

    public async Task<MembershipResponse?> LeaveAsync(LeaveCommunityRequest request, CancellationToken cancellationToken = default)
    {
        var membership = await _membershipRepository.GetByIdAsync(new MembershipId(request.MembershipId), cancellationToken);

        if (membership is null)
        {
            return null;
        }

        membership.Leave(DateTime.UtcNow);
        await _membershipRepository.UpdateAsync(membership, cancellationToken);
        return Map(membership, isInvite: false);
    }

    public async Task<MembershipResponse?> AppointModeratorAsync(AppointModeratorRequest request, CancellationToken cancellationToken = default)
    {
        var membership = await _membershipRepository.GetByIdAsync(new MembershipId(request.MembershipId), cancellationToken);

        if (membership is null)
        {
            return null;
        }

        membership.AppointModerator(DateTime.UtcNow);
        await _membershipRepository.UpdateAsync(membership, cancellationToken);
        return Map(membership, isInvite: false);
    }

    public async Task<MembershipResponse?> RemoveModeratorAsync(RemoveModeratorRequest request, CancellationToken cancellationToken = default)
    {
        var membership = await _membershipRepository.GetByIdAsync(new MembershipId(request.MembershipId), cancellationToken);

        if (membership is null)
        {
            return null;
        }

        membership.RemoveModerator(DateTime.UtcNow);
        await _membershipRepository.UpdateAsync(membership, cancellationToken);
        return Map(membership, isInvite: false);
    }

    private static MembershipResponse Map(CommunityMembership membership, bool isInvite)
        => new(
            membership.Id.Value,
            membership.CommunityId.Value,
            membership.UserId.Value,
            membership.Role,
            membership.JoinedAt,
            membership.LeftAt,
            isInvite);
}
