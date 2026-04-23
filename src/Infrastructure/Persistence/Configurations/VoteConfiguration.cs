using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.ToTable("votes", "forum");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new VoteId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TargetType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.TargetId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .IsRequired();

        builder.Property(x => x.Direction)
            .HasConversion(direction => (short)direction, value => (VoteDirection)value)
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(x => x.CastAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.RetractedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(x => new { x.TargetType, x.TargetId, x.UserId })
            .IsUnique();

        builder.Ignore(x => x.DomainEvents);
    }
}