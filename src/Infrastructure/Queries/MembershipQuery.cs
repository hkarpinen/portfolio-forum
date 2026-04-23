using Forum.Application.Queries;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Queries;

internal sealed class MembershipQuery : IMembershipQuery
{
    private readonly ForumDbContext _db;

    public MembershipQuery(ForumDbContext db) => _db = db;

    public async Task<bool> IsMemberAsync(Guid communityId, Guid userId, CancellationToken cancellationToken = default)
    {
        var membership = await _db.Memberships.FirstOrDefaultAsync(
            m => m.UserId == new UserId(userId) && m.CommunityId == new CommunityId(communityId),
            cancellationToken);
        return membership is not null && membership.LeftAt is null;
    }
}
