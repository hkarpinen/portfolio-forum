using Forum.Application.Commands;

namespace Forum.Application.Managers;

public interface ICommentWorkflowManager
{
    Task<Guid> CreateAsync(CreateCommentCommand command, CancellationToken cancellationToken = default);
    Task<bool> EditAsync(EditCommentCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(DeleteCommentCommand command, CancellationToken cancellationToken = default);
}
