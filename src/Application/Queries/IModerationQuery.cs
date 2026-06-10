using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Queries;

public interface IModerationQuery
{
    Task<ModerationQueueDto> QueueAsync(ModerationQueueCommand command, CancellationToken cancellationToken = default);
    Task<ModerationQueueDto> QueueBySlugAsync(string communitySlug, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ModerationLogListDto> ListLogAsync(string communitySlug, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(Guid CommunityId, Guid ThreadId)?> GetThreadCommunityIdAsync(Guid threadId, CancellationToken cancellationToken = default);
    Task<Guid?> GetCommentCommunityIdAsync(Guid commentId, CancellationToken cancellationToken = default);
}
