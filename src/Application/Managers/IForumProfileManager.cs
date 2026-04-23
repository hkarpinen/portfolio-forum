using Forum.Application.Contracts;

namespace Forum.Application.Managers;

public interface IForumProfileManager
{
    Task<ForumProfileResponse> UpsertAsync(UpdateForumProfileRequest request, CancellationToken cancellationToken = default);
}
