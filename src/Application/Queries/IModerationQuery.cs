using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Queries;

public interface IModerationQuery
{
    Task<ModerationQueueDto> QueueAsync(ModerationQueueCommand command, CancellationToken cancellationToken = default);
}
