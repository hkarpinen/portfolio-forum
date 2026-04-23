using Forum.Application.Contracts;

namespace Forum.Application.Queries;

public interface IThreadQuery
{
    Task<ThreadListResponse> ListAsync(ListThreadsRequest request, CancellationToken cancellationToken = default);
    Task<ThreadResponse?> GetDetailAsync(ThreadDetailRequest request, CancellationToken cancellationToken = default);
}
