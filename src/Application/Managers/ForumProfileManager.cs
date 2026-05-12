using Forum.Application.Commands;
using Forum.Application.Dtos;
using Forum.Application.Mappers;
using Forum.Domain.Aggregates;
using Forum.Application.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class ForumProfileManager : IForumProfileManager
{
    private readonly IForumProfileRepository _repository;

    public ForumProfileManager(IForumProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<ForumProfileDto> UpsertAsync(UpdateForumProfileCommand command, CancellationToken cancellationToken = default)
    {
        var userId = new UserId(command.UserId);
        var profile = await _repository.GetByUserIdAsync(userId, cancellationToken);

        if (profile is null)
        {
            profile = ForumProfile.Create(userId, command.Bio, command.Signature);
            await _repository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.Update(command.Bio, command.Signature, DateTime.UtcNow);
            await _repository.UpdateAsync(profile, cancellationToken);
        }

        await _repository.CommitAsync(cancellationToken);
        return ForumProfileMapper.ToDto(profile);
    }
}
