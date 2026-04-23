using Forum.Application.Contracts;
using Forum.Application.Queries;
using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Queries;

internal sealed class ForumProfileQuery : IForumProfileQuery
{
    private readonly ForumDbContext _db;

    public ForumProfileQuery(ForumDbContext db) => _db = db;

    public async Task<ForumProfileResponse?> GetAsync(GetForumProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _db.ForumProfiles.FirstOrDefaultAsync(p => p.UserId == new UserId(request.UserId), cancellationToken);
        return profile is null ? null : Map(profile);
    }

    private static ForumProfileResponse Map(ForumProfile p) => new(
        p.UserId.Value,
        p.Bio,
        p.Signature,
        p.CreatedAt,
        p.UpdatedAt);
}
