using Forum.Domain;
using Forum.Domain.Aggregates;
using Infrastructure.Persistence.Projections;
using MassTransit;
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
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<UserProjection> UserProjections => Set<UserProjection>();
    public DbSet<ForumProfile> ForumProfiles => Set<ForumProfile>();

    public ForumDbContext(DbContextOptions<ForumDbContext> options)
        : base(options)
    {
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("forum");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ForumDbContext).Assembly);

        // MassTransit's transactional outbox and inbox, replacing the hand-rolled outbox_messages
        // table, its polling publisher, and the processed_events dedup.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
