using Forum.Application.Contracts;

namespace Forum.Application.Queries;

public interface ICommunityQuery
{
    Task<CommunityListResponse> ListAsync(ListCommunitiesRequest request, CancellationToken cancellationToken = default);
    Task<CommunityResponse?> GetDetailAsync(CommunityDetailRequest request, CancellationToken cancellationToken = default);
    Task<CommunityResponse?> GetByNameAsync(CommunityByNameRequest request, CancellationToken cancellationToken = default);
}
