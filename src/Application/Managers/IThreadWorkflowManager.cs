using Forum.Application.Contracts;

namespace Forum.Application.Managers;

public interface IThreadWorkflowManager
{
    Task<ThreadMutationResponse> CreateAsync(CreateThreadRequest request, CancellationToken cancellationToken = default);
    Task<ThreadMutationResponse?> EditAsync(EditThreadRequest request, CancellationToken cancellationToken = default);
    Task<ThreadMutationResponse?> DeleteAsync(DeleteThreadRequest request, CancellationToken cancellationToken = default);
    Task<ThreadMutationResponse?> LockAsync(LockThreadRequest request, CancellationToken cancellationToken = default);
    Task<ThreadMutationResponse?> PinAsync(PinThreadRequest request, CancellationToken cancellationToken = default);
}
