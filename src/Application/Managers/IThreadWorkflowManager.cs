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

    // Draft authoring lifecycle. A draft is a thread in `ThreadStatus.Draft`;
    // these methods drive the transitions. The thread becomes publicly
    // visible at `PublishDraftAsync`, which is when `ThreadCreated` fires.
    Task<ThreadMutationDto> BeginDraftAsync(BeginDraftCommand command, CancellationToken cancellationToken = default);
    Task<ThreadMutationDto?> ReviseDraftAsync(ReviseDraftCommand command, CancellationToken cancellationToken = default);
    Task<ThreadMutationDto?> PublishDraftAsync(PublishDraftCommand command, CancellationToken cancellationToken = default);
    Task<bool> AbandonDraftAsync(AbandonDraftCommand command, CancellationToken cancellationToken = default);
}
