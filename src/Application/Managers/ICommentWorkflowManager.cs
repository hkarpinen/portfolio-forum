using Forum.Application.Contracts;

namespace Forum.Application.Managers;

public interface ICommentWorkflowManager
{
    Task<Guid> CreateAsync(CreateCommentRequest request, CancellationToken cancellationToken = default);
    Task<bool> EditAsync(EditCommentRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(DeleteCommentRequest request, CancellationToken cancellationToken = default);
}
