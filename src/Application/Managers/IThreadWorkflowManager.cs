using Forum.Application.Contracts;

namespace Forum.Application.Managers;

public interface IThreadWorkflowManager
{
    Task<ThreadResponse> CreateAsync(CreateThreadRequest request, CancellationToken cancellationToken = default);
    Task<ThreadResponse?> EditAsync(EditThreadRequest request, CancellationToken cancellationToken = default);
    Task<ThreadResponse?> DeleteAsync(DeleteThreadRequest request, CancellationToken cancellationToken = default);
    Task<ThreadResponse?> LockAsync(LockThreadRequest request, CancellationToken cancellationToken = default);
    Task<ThreadResponse?> PinAsync(PinThreadRequest request, CancellationToken cancellationToken = default);
}
