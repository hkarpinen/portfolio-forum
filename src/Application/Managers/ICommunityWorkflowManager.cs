using Forum.Application.Contracts;

namespace Forum.Application.Managers;

public interface ICommunityWorkflowManager
{
    Task<CommunityResponse> CreateAsync(CreateCommunityRequest request, CancellationToken cancellationToken = default);
    Task<CommunityResponse?> UpdateAsync(UpdateCommunityRequest request, CancellationToken cancellationToken = default);
    Task<CommunityResponse?> TransferOwnershipAsync(TransferCommunityOwnershipRequest request, CancellationToken cancellationToken = default);
}
