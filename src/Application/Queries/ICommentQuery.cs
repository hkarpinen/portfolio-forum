using Forum.Application.Contracts;

namespace Forum.Application.Queries;

public interface ICommentQuery
{
    Task<CommentTreeResponse> ListTreeAsync(ListCommentTreeRequest request, CancellationToken cancellationToken = default);
    Task<ProfileCommentListResponse> ListByAuthorAsync(Guid authorId, int page, int pageSize, CancellationToken cancellationToken = default);
}
