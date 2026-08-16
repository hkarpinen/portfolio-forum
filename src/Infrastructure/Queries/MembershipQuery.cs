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
        return await _db.Memberships.AnyAsync(
            m => m.UserId == new UserId(userId) && m.CommunityId == new CommunityId(communityId),
            cancellationToken);
    }

    public async Task<(bool IsMember, string? Role)> GetMembershipAsync(Guid communityId, Guid userId, CancellationToken cancellationToken = default)
    {
        var membership = await _db.Memberships.FirstOrDefaultAsync(
            m => m.UserId == new UserId(userId) && m.CommunityId == new CommunityId(communityId),
            cancellationToken);
        if (membership is null)
            return (false, null);
        return (true, membership.Role.ToString());
    }

    public async Task<IReadOnlyList<CommunityMemberItem>> ListByCommunityAsync(Guid communityId, CancellationToken cancellationToken = default)
    {
        var memberships = await _db.Memberships
            .Where(m => m.CommunityId == new CommunityId(communityId))
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);

        // Compared as the strongly-typed id, not `.Value`. EF translates the value conversion on
        // the id itself; unwrapping it first compiles and then throws at runtime, which is a 500 on
        // every screen that shows a member's name.
        var userIds = memberships.Select(m => m.UserId).ToHashSet();
        var projections = await _db.UserProjections
            .Where(p => userIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var profileMap = projections.ToDictionary(p => p.Id.Value);

        return memberships.Select(m =>
        {
            profileMap.TryGetValue(m.UserId.Value, out var proj);
            return new CommunityMemberItem(
                m.Id.Value,
                m.UserId.Value,
                proj?.EffectiveName,
                proj?.AvatarUrl,
                m.Role.ToString(),
                m.JoinedAt);
        }).ToList();
    }

    public async Task<IReadOnlyList<UserCommunityItem>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var memberships = await _db.Memberships
            .Where(m => m.UserId == new UserId(userId))
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);

        var communityIds = memberships.Select(m => m.CommunityId).ToHashSet();
        var communities = await _db.Communities
            .Where(c => communityIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        var communityMap = communities.ToDictionary(c => c.Id.Value);

        return memberships.Select(m =>
        {
            communityMap.TryGetValue(m.CommunityId.Value, out var community);
            return new UserCommunityItem(
                m.Id.Value,
                m.CommunityId.Value,
                community?.Name ?? m.CommunityId.Value.ToString(),
                community?.Slug ?? m.CommunityId.Value.ToString(),
                community?.ImageUrl,
                m.Role.ToString(),
                m.JoinedAt);
        }).ToList();
    }
}
