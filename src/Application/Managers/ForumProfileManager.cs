using Forum.Application.Contracts;
using Forum.Domain.Aggregates;
using Forum.Domain.Repositories;
using Forum.Domain.ValueObjects;

namespace Forum.Application.Managers;

internal sealed class ForumProfileManager : IForumProfileManager
{
    private readonly IForumProfileRepository _repository;

    public ForumProfileManager(IForumProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<ForumProfileResponse> UpsertAsync(UpdateForumProfileRequest request, CancellationToken cancellationToken = default)
    {
        var userId = new UserId(request.UserId);
        var profile = await _repository.GetByUserIdAsync(userId, cancellationToken);

        if (profile is null)
        {
            profile = ForumProfile.Create(userId, request.Bio, request.Signature);
            await _repository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.Update(request.Bio, request.Signature, DateTime.UtcNow);
            await _repository.UpdateAsync(profile, cancellationToken);
        }

        return Map(profile);
    }

    private static ForumProfileResponse Map(ForumProfile profile)
        => new(
            profile.UserId.Value,
            profile.Bio,
            profile.Signature,
            profile.CreatedAt,
            profile.UpdatedAt);
}
