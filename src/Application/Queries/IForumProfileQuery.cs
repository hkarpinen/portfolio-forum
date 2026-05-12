using Forum.Application.Commands;
using Forum.Application.Dtos;

namespace Forum.Application.Queries;

public interface IForumProfileQuery
{
    Task<ForumProfileDto?> GetAsync(GetForumProfileCommand command, CancellationToken cancellationToken = default);
}
