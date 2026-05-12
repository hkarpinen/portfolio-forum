using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Queries;

public interface ICommunityQuery
{
    Task<CommunityListDto> ListAsync(ListCommunitiesCommand command, CancellationToken cancellationToken = default);
    Task<CommunityDto?> GetDetailAsync(CommunityDetailCommand command, CancellationToken cancellationToken = default);
    Task<CommunityDto?> GetBySlugAsync(CommunityBySlugCommand command, CancellationToken cancellationToken = default);
}
