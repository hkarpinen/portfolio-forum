using Forum.Application.Contracts;
using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
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

    public async Task<CommunityResponse> CreateAsync(CreateCommunityRequest request, CancellationToken cancellationToken = default)
    {
        var slug = await ResolveUniqueSlugAsync(request.Name, existingCommunityId: null, cancellationToken);

        var community = Community.Create(
            request.Name,
            slug,
            request.Visibility,
            new UserId(request.OwnerId),
            request.Description,
            request.ImageUrl);

        await _communityRepository.AddAsync(community, cancellationToken);

        // Create an Owner membership for the creator so they can manage the community
        var ownerMembership = CommunityMembership.Create(
            community.Id,
            new UserId(request.OwnerId),
            CommunityRole.Owner);
        await _membershipRepository.AddAsync(ownerMembership, cancellationToken);

        return Map(community);
    }

    public async Task<CommunityResponse?> UpdateAsync(UpdateCommunityRequest request, CancellationToken cancellationToken = default)
    {
        var communityId = new CommunityId(request.CommunityId);
        var community = await _communityRepository.GetByIdAsync(communityId, cancellationToken);

        if (community is null)
            return null;

        // Resource-level auth: caller must be community Owner/Moderator or global Admin.
        if (!request.RequestingUserIsAdmin)
        {
            var membership = await _membershipRepository.GetByUserAndCommunityAsync(
                new UserId(request.RequestingUserId), communityId, cancellationToken);

            if (membership?.Role is not (CommunityRole.Owner or CommunityRole.Moderator))
                throw new UnauthorizedAccessException("Only the community owner, a moderator, or a global admin can update this community.");
        }

        var slug = community.Slug;
        if (!string.Equals(community.Name, request.Name, StringComparison.Ordinal))
        {
            slug = await ResolveUniqueSlugAsync(request.Name, existingCommunityId: community.Id, cancellationToken);
        }

        community.Update(request.Name, slug, request.Visibility, DateTime.UtcNow, request.Description, request.ImageUrl);
        await _communityRepository.UpdateAsync(community, cancellationToken);
        return Map(community);
    }

    public async Task<CommunityResponse?> TransferOwnershipAsync(TransferCommunityOwnershipRequest request, CancellationToken cancellationToken = default)
    {
        var communityId = new CommunityId(request.CommunityId);
        var community = await _communityRepository.GetByIdAsync(communityId, cancellationToken);

        if (community is null)
            return null;

        community.TransferOwnership(new UserId(request.NewOwnerId), DateTime.UtcNow);
        await _communityRepository.UpdateAsync(community, cancellationToken);
        return Map(community);
    }

    public async Task<bool> DeleteAsync(DeleteCommunityRequest request, CancellationToken cancellationToken = default)
    {
        var communityId = new CommunityId(request.CommunityId);
        var community = await _communityRepository.GetByIdAsync(communityId, cancellationToken);

        if (community is null)
            return false;

        // Resource-level auth: only the owner or a global admin can delete.
        if (!request.RequestingUserIsAdmin && community.OwnerId.Value != request.RequestedByUserId)
            throw new UnauthorizedAccessException("Only the community owner or a global admin can delete this community.");

        community.Delete(DateTime.UtcNow);
        await _communityRepository.DeleteAsync(communityId, cancellationToken);
        return true;
    }

    private static CommunityResponse Map(Community community)
        => new(
            community.Id.Value,
            community.Slug,
            community.Name,
            community.Description,
            community.ImageUrl,
            community.Visibility,
            community.OwnerId.Value,
            community.CreatedAt,
            community.UpdatedAt);

    private async Task<string> ResolveUniqueSlugAsync(string name, CommunityId? existingCommunityId, CancellationToken cancellationToken)
    {
        var baseSlug = Community.Slugify(name);
        if (string.IsNullOrEmpty(baseSlug))
        {
            // All non-alphanum chars (e.g. emoji-only name). Fall back to a random token.
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
