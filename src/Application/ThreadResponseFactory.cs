using Forum.Application.Dtos;
using Forum.Domain.Aggregates;

namespace Forum.Application;

/// <summary>Constructs write-side mutation response DTOs from domain aggregates.</summary>
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
