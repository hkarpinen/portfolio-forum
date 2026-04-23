using Forum.Domain.Aggregates;
using Forum.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class ForumDbContext : DbContext
{
    public DbSet<Community> Communities => Set<Community>();
    public DbSet<CommunityMembership> Memberships => Set<CommunityMembership>();
    public DbSet<CommunityBan> Bans => Set<CommunityBan>();
    public DbSet<ForumThread> Threads => Set<ForumThread>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<ModerationLog> ModerationLogs => Set<ModerationLog>();
    public DbSet<UserProjection> UserProjections => Set<UserProjection>();
    public DbSet<ForumProfile> ForumProfiles => Set<ForumProfile>();
    public DbSet<ForumNotification> Notifications => Set<ForumNotification>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    public ForumDbContext(DbContextOptions<ForumDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("forum");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ForumDbContext).Assembly);
    }
}