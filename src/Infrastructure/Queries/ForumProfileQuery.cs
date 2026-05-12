using Forum.Application.Commands;
using Forum.Application.Dtos;
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

    public async Task<ForumProfileDto?> GetAsync(GetForumProfileCommand request, CancellationToken cancellationToken = default)
    {
        var userId = new UserId(request.UserId);
        var profile = await _db.ForumProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        var proj = await _db.UserProjections.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);
        if (profile is null && proj is null) return null;
        return new ForumProfileDto(
            request.UserId,
            proj?.EffectiveName,
            proj?.AvatarUrl,
            profile?.Bio,
            profile?.Signature,
            profile?.CreatedAt ?? DateTime.UtcNow,
            profile?.UpdatedAt);
    }
}
