using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Queries;

public interface ICommentQuery
{
    Task<CommentTreeDto> ListTreeAsync(ListCommentTreeCommand command, CancellationToken cancellationToken = default);
    Task<ProfileCommentListDto> ListByAuthorAsync(Guid authorId, int page, int pageSize, CancellationToken cancellationToken = default);
}
