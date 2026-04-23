using Forum.Application.Contracts;
using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class CommunityWorkflowManager : ICommunityWorkflowManager
{
    private readonly ICommunityRepository _communityRepository;

    public CommunityWorkflowManager(ICommunityRepository communityRepository)
    {
        _communityRepository = communityRepository;
    }

    public async Task<CommunityResponse> CreateAsync(CreateCommunityRequest request, CancellationToken cancellationToken = default)
    {
        var community = Community.Create(
            request.Name,
            request.Visibility,
            new UserId(request.OwnerId),
            request.Description,
            request.ImageUrl);

        await _communityRepository.AddAsync(community, cancellationToken);
        return Map(community);
    }

    public async Task<CommunityResponse?> UpdateAsync(UpdateCommunityRequest request, CancellationToken cancellationToken = default)
    {
        var communityId = new CommunityId(request.CommunityId);
        var community = await _communityRepository.GetByIdAsync(communityId, cancellationToken);

        if (community is null)
        {
            return null;
        }

        community.Update(request.Name, request.Visibility, DateTime.UtcNow, request.Description, request.ImageUrl);
        await _communityRepository.UpdateAsync(community, cancellationToken);
        return Map(community);
    }

    public async Task<CommunityResponse?> TransferOwnershipAsync(TransferCommunityOwnershipRequest request, CancellationToken cancellationToken = default)
    {
        var communityId = new CommunityId(request.CommunityId);
        var community = await _communityRepository.GetByIdAsync(communityId, cancellationToken);

        if (community is null)
        {
            return null;
        }

        community.TransferOwnership(new UserId(request.NewOwnerId), DateTime.UtcNow);
        await _communityRepository.UpdateAsync(community, cancellationToken);
        return Map(community);
    }

    private static CommunityResponse Map(Community community)
        => new(
            community.Id.Value,
            community.Name,
            community.Description,
            community.ImageUrl,
            community.Visibility,
            community.OwnerId.Value,
            community.CreatedAt,
            community.UpdatedAt);
}
