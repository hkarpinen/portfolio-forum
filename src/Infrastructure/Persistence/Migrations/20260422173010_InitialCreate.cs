using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "forum");

            migrationBuilder.CreateTable(
                name: "comments",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    thread_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    edited_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('english', coalesce(content, ''))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "communities",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('english', coalesce(name, ''))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_communities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "community_bans",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    community_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    banned_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    unbanned_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_bans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "community_memberships",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    community_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    left_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_memberships", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "moderation_logs",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    community_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    performed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_content = table.Column<string>(type: "text", nullable: true),
                    performed_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moderation_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "processed_events",
                schema: "forum",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_events", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "forum",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    signature = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profiles", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "threads",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    community_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    edited_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('english', coalesce(title, '') || ' ' || coalesce(content, ''))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_threads", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_projections",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    registered_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_banned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_projections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "votes",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<short>(type: "smallint", nullable: false),
                    cast_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    retracted_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_votes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_comments_author_id",
                schema: "forum",
                table: "comments",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_search_vector",
                schema: "forum",
                table: "comments",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_comments_thread_id_created_at",
                schema: "forum",
                table: "comments",
                columns: new[] { "thread_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_communities_name",
                schema: "forum",
                table: "communities",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_communities_owner_id",
                schema: "forum",
                table: "communities",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_communities_search_vector",
                schema: "forum",
                table: "communities",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_community_bans_community_id_user_id",
                schema: "forum",
                table: "community_bans",
                columns: new[] { "community_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_community_memberships_community_id_user_id",
                schema: "forum",
                table: "community_memberships",
                columns: new[] { "community_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_community_memberships_user_id",
                schema: "forum",
                table: "community_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_moderation_logs_community_id_performed_at",
                schema: "forum",
                table: "moderation_logs",
                columns: new[] { "community_id", "performed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_moderation_logs_performed_by",
                schema: "forum",
                table: "moderation_logs",
                column: "performed_by");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_read_created_at",
                schema: "forum",
                table: "notifications",
                columns: new[] { "user_id", "read", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_processed_events_processed_at",
                schema: "forum",
                table: "processed_events",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "ix_threads_author_id",
                schema: "forum",
                table: "threads",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_threads_community_id_created_at",
                schema: "forum",
                table: "threads",
                columns: new[] { "community_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_threads_search_vector",
                schema: "forum",
                table: "threads",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_user_projections_user_name",
                schema: "forum",
                table: "user_projections",
                column: "user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_votes_target_type_target_id_user_id",
                schema: "forum",
                table: "votes",
                columns: new[] { "target_type", "target_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comments",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "communities",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "community_bans",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "community_memberships",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "moderation_logs",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "processed_events",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "profiles",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "threads",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "user_projections",
                schema: "forum");

            migrationBuilder.DropTable(
                name: "votes",
                schema: "forum");
        }
    }
}
