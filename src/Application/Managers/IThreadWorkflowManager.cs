using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Managers;

public interface IThreadWorkflowManager
{
    Task<ThreadMutationDto> CreateAsync(CreateThreadCommand command, CancellationToken cancellationToken = default);
    Task<ThreadMutationDto?> EditAsync(EditThreadCommand command, CancellationToken cancellationToken = default);
    Task<ThreadMutationDto?> DeleteAsync(DeleteThreadCommand command, CancellationToken cancellationToken = default);
    Task<ThreadMutationDto?> LockAsync(LockThreadCommand command, CancellationToken cancellationToken = default);
    Task<ThreadMutationDto?> PinAsync(PinThreadCommand command, CancellationToken cancellationToken = default);
}
