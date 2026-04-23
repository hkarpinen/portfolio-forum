using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class ForumProfileConfiguration : IEntityTypeConfiguration<ForumProfile>
{
    public void Configure(EntityTypeBuilder<ForumProfile> builder)
    {
        builder.ToTable("profiles", "forum");
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Bio)
            .HasMaxLength(ForumProfile.MaxBioLength);

        builder.Property(x => x.Signature)
            .HasMaxLength(ForumProfile.MaxSignatureLength);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.Ignore(x => x.DomainEvents);
    }
}
