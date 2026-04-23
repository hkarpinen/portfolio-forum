using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Infrastructure.Persistence.Configurations;

internal sealed class CommunityConfiguration : IEntityTypeConfiguration<Community>
{
    public void Configure(EntityTypeBuilder<Community> builder)
    {
        builder.ToTable("communities", "forum");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new CommunityId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(2048);

        builder.Property(x => x.Visibility)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.OwnerId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.Property<NpgsqlTsVector>("search_vector")
            .HasColumnType("tsvector")
            .HasComputedColumnSql("to_tsvector('english', coalesce(name, ''))", stored: true);

        builder.HasIndex("search_vector")
            .HasMethod("GIN");

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.OwnerId);

        builder.Ignore(x => x.DomainEvents);
    }
}