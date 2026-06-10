using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Mappers;
using Forum.Domain.Aggregates;
using Forum.Application.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class CommunityWorkflowManager : ICommunityWorkflowManager
{
    private readonly ICommunityRepository _communityRepository;
    private readonly IMembershipRepository _membershipRepository;

    public CommunityWorkflowManager(ICommunityRepository communityRepository, IMembershipRepository membershipRepository)
    {
        _communityRepository = communityRepository;
        _membershipRepository = membershipRepository;
    }

    public async Task<CommunityDto> CreateAsync(CreateCommunityCommand command, CancellationToken cancellationToken = default)
    {
        var slug = await ResolveUniqueSlugAsync(command.Name, existingCommunityId: null, cancellationToken);

        var community = Community.Create(
            command.Name,
            slug,
            command.Visibility,
            new UserId(command.OwnerId),
            command.Description,
            command.ImageUrl,
            command.Rules);

        await _communityRepository.AddAsync(community, cancellationToken);

        var ownerMembership = CommunityMembership.Create(
            community.Id,
            new UserId(command.OwnerId),
            CommunityRole.Owner);
        await _membershipRepository.AddAsync(ownerMembership, cancellationToken);
        await _communityRepository.CommitAsync(cancellationToken);

        return CommunityMapper.ToDto(community);
    }

    public async Task<CommunityDto?> UpdateAsync(UpdateCommunityCommand command, CancellationToken cancellationToken = default)
    {
        var communityId = new CommunityId(command.CommunityId);
        var community = await _communityRepository.GetByIdAsync(communityId, cancellationToken);

        if (community is null)
            return null;

        if (!command.RequestingUserIsAdmin)
        {
            var membership = await _membershipRepository.GetByUserAndCommunityAsync(
                new UserId(command.RequestingUserId), communityId, cancellationToken);

            if (membership?.Role is not (CommunityRole.Owner or CommunityRole.Moderator))
                throw new UnauthorizedAccessException("Only the community owner, a moderator, or a global admin can update this community.");
        }

        var slug = community.Slug;
        if (!string.Equals(community.Name, command.Name, StringComparison.Ordinal))
        {
            slug = await ResolveUniqueSlugAsync(command.Name, existingCommunityId: community.Id, cancellationToken);
        }

        community.Update(command.Name, slug, command.Visibility, DateTime.UtcNow, command.Description, command.ImageUrl, command.Rules);
        await _communityRepository.UpdateAsync(community, cancellationToken);
        await _communityRepository.CommitAsync(cancellationToken);
        return CommunityMapper.ToDto(community);
    }

    public async Task<CommunityDto?> TransferOwnershipAsync(TransferCommunityOwnershipCommand command, CancellationToken cancellationToken = default)
    {
        var communityId = new CommunityId(command.CommunityId);
        var community = await _communityRepository.GetByIdAsync(communityId, cancellationToken);

        if (community is null)
            return null;

        community.TransferOwnership(new UserId(command.NewOwnerId), DateTime.UtcNow);
        await _communityRepository.UpdateAsync(community, cancellationToken);
        await _communityRepository.CommitAsync(cancellationToken);
        return CommunityMapper.ToDto(community);
    }

    public async Task<bool> DeleteAsync(DeleteCommunityCommand command, CancellationToken cancellationToken = default)
    {
        var communityId = new CommunityId(command.CommunityId);
        var community = await _communityRepository.GetByIdAsync(communityId, cancellationToken);

        if (community is null)
            return false;

        if (!command.RequestingUserIsAdmin && community.OwnerId.Value != command.RequestedByUserId)
            throw new UnauthorizedAccessException("Only the community owner or a global admin can delete this community.");

        community.Delete(DateTime.UtcNow);
        await _communityRepository.DeleteAsync(communityId, cancellationToken);
        await _communityRepository.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<string> ResolveUniqueSlugAsync(string name, CommunityId? existingCommunityId, CancellationToken cancellationToken)
    {
        var baseSlug = Community.Slugify(name);
        if (string.IsNullOrEmpty(baseSlug))
        {
            baseSlug = $"c-{Guid.NewGuid().ToString("N")[..8]}";
        }

        var candidate = baseSlug;
        var suffix = 2;
        while (true)
        {
            var existing = await _communityRepository.GetBySlugAsync(candidate, cancellationToken);
            if (existing is null || (existingCommunityId is not null && existing.Id == existingCommunityId))
            {
                return candidate;
            }
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }
    }
}
