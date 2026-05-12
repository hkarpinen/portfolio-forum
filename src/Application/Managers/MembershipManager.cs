using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Mappers;
using Forum.Domain.Aggregates;
using Forum.Application.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class MembershipManager : IMembershipManager
{
    private readonly IMembershipRepository _membershipRepository;

    public MembershipManager(IMembershipRepository membershipRepository)
    {
        _membershipRepository = membershipRepository;
    }

    public async Task<MembershipDto> JoinAsync(JoinCommunityCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await _membershipRepository.GetByUserAndCommunityAsync(
            new UserId(command.UserId), new CommunityId(command.CommunityId), cancellationToken);

        if (existing is not null)
            return ForumMembershipMapper.ToDto(existing, isInvite: false);

        var membership = CommunityMembership.Create(
            new CommunityId(command.CommunityId),
            new UserId(command.UserId));

        await _membershipRepository.AddAsync(membership, cancellationToken);
        await _membershipRepository.CommitAsync(cancellationToken);
        return ForumMembershipMapper.ToDto(membership, isInvite: false);
    }

    public async Task<MembershipDto> InviteAsync(InviteMemberCommand command, CancellationToken cancellationToken = default)
    {
        var membership = CommunityMembership.Create(
            new CommunityId(command.CommunityId),
            new UserId(command.UserId));

        await _membershipRepository.AddAsync(membership, cancellationToken);
        await _membershipRepository.CommitAsync(cancellationToken);
        return ForumMembershipMapper.ToDto(membership, isInvite: true);
    }

    public async Task<MembershipDto?> LeaveAsync(LeaveCommunityCommand command, CancellationToken cancellationToken = default)
    {
        var membership = await _membershipRepository.GetByIdAsync(new MembershipId(command.MembershipId), cancellationToken);
        if (membership is null)
            return null;

        var dto = ForumMembershipMapper.ToDto(membership, isInvite: false);
        await _membershipRepository.DeleteAsync(membership.Id, cancellationToken);
        await _membershipRepository.CommitAsync(cancellationToken);
        return dto;
    }

    public async Task<MembershipDto?> AppointModeratorAsync(AppointModeratorCommand command, CancellationToken cancellationToken = default)
    {
        var membership = await _membershipRepository.GetByIdAsync(new MembershipId(command.MembershipId), cancellationToken);

        if (membership is null)
            return null;

        membership.AppointModerator(DateTime.UtcNow);
        await _membershipRepository.UpdateAsync(membership, cancellationToken);
        await _membershipRepository.CommitAsync(cancellationToken);
        return ForumMembershipMapper.ToDto(membership, isInvite: false);
    }

    public async Task<MembershipDto?> RemoveModeratorAsync(RemoveModeratorCommand command, CancellationToken cancellationToken = default)
    {
        var membership = await _membershipRepository.GetByIdAsync(new MembershipId(command.MembershipId), cancellationToken);

        if (membership is null)
            return null;

        membership.RemoveModerator(DateTime.UtcNow);
        await _membershipRepository.UpdateAsync(membership, cancellationToken);
        await _membershipRepository.CommitAsync(cancellationToken);
        return ForumMembershipMapper.ToDto(membership, isInvite: false);
    }
}
