using Forum.Application.Contracts;
using Forum.Application.Queries;

namespace Infrastructure.Queries;

internal sealed class ModerationQuery : IModerationQuery
{
    public Task<ModerationQueueResponse> QueueAsync(ModerationQueueRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: add moderation queue repository and projection.
        var empty = new ModerationQueueResponse(Array.Empty<ModerationQueueItemResponse>(), 0);
        return Task.FromResult(empty);
    }
}
