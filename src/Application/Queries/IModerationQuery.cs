using Forum.Application.Contracts;

namespace Forum.Application.Queries;

public interface IModerationQuery
{
    Task<ModerationQueueResponse> QueueAsync(ModerationQueueRequest request, CancellationToken cancellationToken = default);
}
