using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Managers;

public interface IForumProfileManager
{
    Task<ForumProfileDto> UpsertAsync(UpdateForumProfileCommand command, CancellationToken cancellationToken = default);
}
