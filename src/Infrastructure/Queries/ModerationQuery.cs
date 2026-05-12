using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Queries;

namespace Infrastructure.Queries;

internal sealed class ModerationQuery : IModerationQuery
{
    public Task<ModerationQueueDto> QueueAsync(ModerationQueueCommand request, CancellationToken cancellationToken = default)
    {
        // TODO: add moderation queue repository and projection.
        var empty = new ModerationQueueDto(Array.Empty<ModerationQueueItemDto>(), 0);
        return Task.FromResult(empty);
    }
}
