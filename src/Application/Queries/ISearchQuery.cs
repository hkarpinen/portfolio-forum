using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Queries;

public interface ISearchQuery
{
    Task<SearchDto> QueryAsync(SearchQueryCommand command, CancellationToken cancellationToken = default);
}
