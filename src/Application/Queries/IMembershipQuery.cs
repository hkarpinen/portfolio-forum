namespace Forum.Application.Queries;

public interface IMembershipQuery
{
    Task<bool> IsMemberAsync(Guid communityId, Guid userId, CancellationToken cancellationToken = default);
}
