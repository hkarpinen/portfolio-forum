using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class CommunityMembershipConfiguration : IEntityTypeConfiguration<CommunityMembership>
{
    public void Configure(EntityTypeBuilder<CommunityMembership> builder)
    {
        builder.ToTable("community_memberships", "forum");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new MembershipId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.CommunityId)
            .HasConversion(id => id.Value, value => new CommunityId(value))
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .IsRequired();

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.JoinedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.LeftAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(x => new { x.CommunityId, x.UserId })
            .IsUnique();

        builder.HasIndex(x => x.UserId);

        builder.Ignore(x => x.DomainEvents);
    }
}