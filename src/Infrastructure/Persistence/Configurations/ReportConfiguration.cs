using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports", "forum");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ReportId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.CommunityId)
            .HasConversion(id => id.Value, value => new CommunityId(value))
            .IsRequired();

        builder.Property(x => x.TargetType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.TargetId)
            .IsRequired();

        builder.Property(x => x.ReporterId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Details)
            .HasColumnType("text");

        builder.Property(x => x.ReportedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ResolvedAt)
            .HasColumnType("timestamptz");

        builder.Property(x => x.ResolvedByUserId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? new UserId(value.Value) : null);

        builder.HasIndex(x => new { x.CommunityId, x.Status, x.ReportedAt });
        builder.HasIndex(x => x.ReporterId);

        builder.Ignore(x => x.DomainEvents);
    }
}
