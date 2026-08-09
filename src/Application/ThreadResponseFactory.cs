using Forum.Application.Dtos;
using Forum.Domain.Aggregates;

namespace Forum.Application;

internal static class ThreadResponseFactory
{
    public static ThreadMutationDto ToMutation(ForumThread thread) =>
        new(
            thread.Id.Value,
            thread.IsLocked,
            thread.IsPinned,
            thread.EditedAt,
            thread.DeletedAt);

}
