using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class ModerationLogConfiguration : IEntityTypeConfiguration<ModerationLog>
{
    public void Configure(EntityTypeBuilder<ModerationLog> builder)
    {
        builder.ToTable("moderation_logs", "forum");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new LogId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.CommunityId)
            .HasConversion(id => id.Value, value => new CommunityId(value))
            .IsRequired();

        builder.Property(x => x.Action)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.PerformedBy)
            .HasConversion(id => id.Value, value => new UserId(value))
            .IsRequired();

        builder.Property(x => x.TargetUserId)
            .HasConversion(id => id != null ? id.Value : (Guid?)null, value => value.HasValue ? new UserId(value.Value) : null);

        builder.Property(x => x.TargetContent)
            .HasColumnType("text");

        builder.Property(x => x.PerformedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(x => new { x.CommunityId, x.PerformedAt });
        builder.HasIndex(x => x.PerformedBy);

        builder.Ignore(x => x.DomainEvents);
    }
}