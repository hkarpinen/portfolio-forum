using Forum.Domain;
using Forum.Domain.Aggregates;
using Infrastructure.Persistence.Projections;
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
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public ForumDbContext(DbContextOptions<ForumDbContext> options)
        : base(options)
    {
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DrainDomainEventsToOutbox();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void DrainDomainEventsToOutbox()
    {
        var aggregates = ChangeTracker.Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
                this.AddToOutbox(domainEvent);
            aggregate.ClearDomainEvents();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("forum");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ForumDbContext).Assembly);
    }
}
