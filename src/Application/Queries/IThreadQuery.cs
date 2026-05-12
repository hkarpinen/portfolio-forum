using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Queries;

public interface IThreadQuery
{
    Task<ThreadListDto> ListAsync(ListThreadsCommand command, CancellationToken cancellationToken = default);
    Task<ThreadListDto> ListByAuthorAsync(Guid authorId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ThreadDto?> GetDetailAsync(ThreadDetailCommand command, CancellationToken cancellationToken = default);
    Task<FeedListDto> ListFeedAsync(FeedCommand command, CancellationToken cancellationToken = default);
    Task<SearchDto> SearchAsync(SearchQueryCommand command, CancellationToken cancellationToken = default);
}
