using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Managers;

public interface ICommentWorkflowManager
{
    Task<Guid> CreateAsync(CreateCommentCommand command, CancellationToken cancellationToken = default);
    Task<CommentDto?> EditAsync(EditCommentCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(DeleteCommentCommand command, CancellationToken cancellationToken = default);
}
