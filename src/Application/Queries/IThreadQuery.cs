using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Queries;

public interface IThreadQuery
{
    // Public-read paths — implementations MUST filter `Status == Published`
    // so drafts don't leak into feeds, search, or community listings.
    Task<ThreadListDto> ListAsync(ListThreadsCommand command, CancellationToken cancellationToken = default);
    Task<ThreadListDto> ListByAuthorAsync(Guid authorId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ThreadDto?> GetDetailAsync(ThreadDetailCommand command, CancellationToken cancellationToken = default);
    Task<FeedListDto> ListFeedAsync(FeedCommand command, CancellationToken cancellationToken = default);
    Task<SearchDto> SearchAsync(SearchQueryCommand command, CancellationToken cancellationToken = default);

    // Author-scoped draft reads. All three must enforce the
    // `Status == Draft AND AuthorId == authorId` filter at the data layer
    // — drafts are private to their author.
    Task<IReadOnlyList<ThreadSummaryDto>> ListDraftsByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default);
    Task<ThreadDto?> GetDraftByIdAsync(Guid authorId, Guid threadId, CancellationToken cancellationToken = default);
    Task<int> CountDraftsByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default);
}
