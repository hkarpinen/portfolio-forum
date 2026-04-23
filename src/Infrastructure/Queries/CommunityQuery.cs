using Forum.Application.Contracts;
using Forum.Application.Queries;
using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Queries;

internal sealed class CommunityQuery : ICommunityQuery
{
    private readonly ForumDbContext _db;

    public CommunityQuery(ForumDbContext db) => _db = db;

    public async Task<CommunityListResponse> ListAsync(ListCommunitiesRequest request, CancellationToken cancellationToken = default)
    {
        var total = await _db.Communities.CountAsync(cancellationToken);
        var items = await _db.Communities
            .OrderBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new CommunityListResponse(items.Select(Map).ToList(), total);
    }

    public async Task<CommunityResponse?> GetDetailAsync(CommunityDetailRequest request, CancellationToken cancellationToken = default)
    {
        var community = await _db.Communities.FirstOrDefaultAsync(c => c.Id == new CommunityId(request.CommunityId), cancellationToken);
        return community is null ? null : Map(community);
    }

    public async Task<CommunityResponse?> GetByNameAsync(CommunityByNameRequest request, CancellationToken cancellationToken = default)
    {
        var community = await _db.Communities.FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower(), cancellationToken);
        return community is null ? null : Map(community);
    }

    private static CommunityResponse Map(Community c) => new(
        c.Id.Value,
        c.Name,
        c.Description,
        c.ImageUrl,
        c.Visibility,
        c.OwnerId.Value,
        c.CreatedAt,
        c.UpdatedAt);
}
