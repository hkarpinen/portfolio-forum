using Forum.Domain.Aggregates;
using Forum.Domain.ReadModels;
using Forum.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class UserProjectionConfiguration : IEntityTypeConfiguration<UserProjection>
{
    public void Configure(EntityTypeBuilder<UserProjection> builder)
    {
        builder.ToTable("user_projections", "forum");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new UserId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.UserName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(120);

        builder.Property(x => x.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(x => x.RegisteredAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.IsBanned)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(x => x.UserName).IsUnique();
    }
}