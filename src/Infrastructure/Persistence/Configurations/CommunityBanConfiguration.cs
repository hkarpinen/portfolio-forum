using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class CommunityBanConfiguration : IEntityTypeConfiguration<CommunityBan>
{
    public void Configure(EntityTypeBuilder<CommunityBan> builder)
    {
        builder.ToTable("community_bans", "forum");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new BanId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.CommunityId)
            .HasConversion(id => id.Value, value => new CommunityId(value))
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .IsRequired();

        builder.Property(x => x.BannedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

        builder.Property(x => x.UnbannedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(x => new { x.CommunityId, x.UserId });

        builder.Ignore(x => x.DomainEvents);
    }
}