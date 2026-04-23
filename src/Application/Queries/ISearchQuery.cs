using Forum.Application.Contracts;

namespace Forum.Application.Queries;

public interface ISearchQuery
{
    Task<SearchResponse> QueryAsync(SearchQueryRequest request, CancellationToken cancellationToken = default);
}
