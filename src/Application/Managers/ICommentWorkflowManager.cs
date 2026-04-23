using Forum.Application.Contracts;

namespace Forum.Application.Managers;

public interface ICommentWorkflowManager
{
    Task<CommentResponse> CreateAsync(CreateCommentRequest request, CancellationToken cancellationToken = default);
    Task<CommentResponse?> EditAsync(EditCommentRequest request, CancellationToken cancellationToken = default);
    Task<CommentResponse?> DeleteAsync(DeleteCommentRequest request, CancellationToken cancellationToken = default);
}
