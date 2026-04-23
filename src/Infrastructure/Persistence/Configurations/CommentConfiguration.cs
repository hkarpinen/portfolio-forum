using Forum.Domain.Aggregates;
using Forum.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Infrastructure.Persistence.Configurations;

internal sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments", "forum");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new CommentId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.ThreadId)
            .HasConversion(id => id.Value, value => new ThreadId(value))
            .IsRequired();

        builder.Property(x => x.AuthorId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.EditedAt)
            .HasColumnType("timestamptz");

        builder.Property(x => x.DeletedAt)
            .HasColumnType("timestamptz");

        builder.Property<NpgsqlTsVector>("search_vector")
            .HasColumnType("tsvector")
            .HasComputedColumnSql("to_tsvector('english', coalesce(content, ''))", stored: true);

        builder.HasIndex("search_vector")
            .HasMethod("GIN");

        builder.HasIndex(x => new { x.ThreadId, x.CreatedAt });
        builder.HasIndex(x => x.AuthorId);

        builder.Ignore(x => x.DomainEvents);
    }
}