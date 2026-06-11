using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ForumModerationAndDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_threads_author_id",
                schema: "forum",
                table: "threads");

            migrationBuilder.DropIndex(
                name: "ix_threads_community_id_created_at",
                schema: "forum",
                table: "threads");

            migrationBuilder.DropColumn(
                name: "flair",
                schema: "forum",
                table: "threads");

            migrationBuilder.AddColumn<DateTime>(
                name: "saved_at",
                schema: "forum",
                table: "threads",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                schema: "forum",
                table: "threads",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<List<string>>(
                name: "tags",
                schema: "forum",
                table: "threads",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<string>(
                name: "rules",
                schema: "forum",
                table: "communities",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "forum",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    community_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    details = table.Column<string>(type: "text", nullable: true),
                    reported_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_threads_author_id_status",
                schema: "forum",
                table: "threads",
                columns: new[] { "author_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_threads_community_id_created_at",
                schema: "forum",
                table: "threads",
                columns: new[] { "community_id", "created_at" },
                filter: "deleted_at IS NULL AND status = 1");

            migrationBuilder.CreateIndex(
                name: "ix_reports_community_id_status_reported_at",
                schema: "forum",
                table: "reports",
                columns: new[] { "community_id", "status", "reported_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_reporter_id",
                schema: "forum",
                table: "reports",
                column: "reporter_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reports",
                schema: "forum");

            migrationBuilder.DropIndex(
                name: "ix_threads_author_id_status",
                schema: "forum",
                table: "threads");

            migrationBuilder.DropIndex(
                name: "ix_threads_community_id_created_at",
                schema: "forum",
                table: "threads");

            migrationBuilder.DropColumn(
                name: "saved_at",
                schema: "forum",
                table: "threads");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "forum",
                table: "threads");

            migrationBuilder.DropColumn(
                name: "tags",
                schema: "forum",
                table: "threads");

            migrationBuilder.DropColumn(
                name: "rules",
                schema: "forum",
                table: "communities");

            migrationBuilder.AddColumn<string>(
                name: "flair",
                schema: "forum",
                table: "threads",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_threads_author_id",
                schema: "forum",
                table: "threads",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_threads_community_id_created_at",
                schema: "forum",
                table: "threads",
                columns: new[] { "community_id", "created_at" },
                filter: "deleted_at IS NULL");
        }
    }
}
