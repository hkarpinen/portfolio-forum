using Forum.Application.Contracts;

namespace Forum.Application.Queries;

public interface IForumProfileQuery
{
    Task<ForumProfileResponse?> GetAsync(GetForumProfileRequest request, CancellationToken cancellationToken = default);
}
