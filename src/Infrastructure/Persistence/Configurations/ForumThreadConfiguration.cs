using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Infrastructure.Persistence.Configurations;

internal sealed class ForumThreadConfiguration : IEntityTypeConfiguration<ForumThread>
{
    public void Configure(EntityTypeBuilder<ForumThread> builder)
    {
        builder.ToTable("threads", "forum");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ThreadId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.CommunityId)
            .HasConversion(id => id.Value, value => new CommunityId(value))
            .IsRequired();

        builder.Property(x => x.AuthorId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.EditedAt)
            .HasColumnType("timestamptz");

        builder.Property(x => x.DeletedAt)
            .HasColumnType("timestamptz");

        builder.Property(x => x.IsLocked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsPinned)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property<NpgsqlTsVector>("search_vector")
            .HasColumnType("tsvector")
            .HasComputedColumnSql("to_tsvector('english', coalesce(title, '') || ' ' || coalesce(content, ''))", stored: true);

        builder.HasIndex("search_vector")
            .HasMethod("GIN");

        builder.HasIndex(x => new { x.CommunityId, x.CreatedAt });
        builder.HasIndex(x => x.AuthorId);

        builder.Ignore(x => x.DomainEvents);
    }
}