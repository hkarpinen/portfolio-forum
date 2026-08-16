using Forum.Domain.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Tests;

/// <summary>
/// That the queries this service runs can actually be turned into SQL.
///
/// `.Where(p => userIds.Contains(p.Id.Value))` on a strongly-typed id compiles and then throws the
/// moment it runs: EF translates the value conversion on the id itself, not on `.Value` unwrapped
/// first. That shipped in MembershipQuery and returned a 500 from every screen listing a
/// community's members — 88 of them in one e2e run — while every unit test stayed green, because
/// none of them goes near a query.
///
/// No database is involved. ToQueryString builds the model and translates the expression, which is
/// the whole of what breaks; a connection string pointing nowhere is enough.
///
/// The rule: compare strongly-typed ids as themselves, never as `.Value`.
/// </summary>
public class QueryTranslationTests
{
    private static ForumDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ForumDbContext>()
            .UseNpgsql("Host=translation-only;Database=none;Username=none;Password=none")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ForumDbContext(options);
    }

    [Fact]
    public void LookingUpUserProjectionsByIdTranslates()
    {
        using var db = NewContext();
        var ids = new HashSet<UserId> { new UserId(Guid.NewGuid()) };

        var sql = db.UserProjections.Where(p => ids.Contains(p.Id)).ToQueryString();

        Assert.Contains("user_projections", sql);
    }

    [Fact]
    public void LookingUpCommunitiesByIdTranslates()
    {
        using var db = NewContext();
        var ids = new HashSet<CommunityId> { new CommunityId(Guid.NewGuid()) };

        var sql = db.Communities.Where(c => ids.Contains(c.Id)).ToQueryString();

        Assert.Contains("communities", sql);
    }
}
