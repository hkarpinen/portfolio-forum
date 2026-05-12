using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Managers;

public interface ICommunityWorkflowManager
{
    Task<CommunityDto> CreateAsync(CreateCommunityCommand command, CancellationToken cancellationToken = default);
    Task<CommunityDto?> UpdateAsync(UpdateCommunityCommand command, CancellationToken cancellationToken = default);
    Task<CommunityDto?> TransferOwnershipAsync(TransferCommunityOwnershipCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(DeleteCommunityCommand command, CancellationToken cancellationToken = default);
}
