using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewbieCoder.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedByUserIdToPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_status",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_posts_published_by_user_id",
                table: "posts");

            migrationBuilder.RenameColumn(
                name: "PasswordChangedAt",
                table: "users",
                newName: "password_changed_at");

            migrationBuilder.RenameColumn(
                name: "published_by_user_id",
                table: "posts",
                newName: "PublishedByUserId");

            migrationBuilder.AddColumn<string>(
                name: "display_title",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "locked_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "locked_by",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locked_reason",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "website_url",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "new_value",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "old_value",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trace_id",
                table: "audit_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "seed_flags",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    seeded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed_flags", x => x.key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_locked_by",
                table: "users",
                column: "locked_by");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_status",
                table: "users",
                sql: "status IN ('ACT','INACT','BAN','CLS','LOCKED')");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_trace_id",
                table: "audit_logs",
                column: "trace_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_locked_by",
                table: "users",
                column: "locked_by",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_users_locked_by",
                table: "users");

            migrationBuilder.DropTable(
                name: "seed_flags");

            migrationBuilder.DropIndex(
                name: "IX_users_locked_by",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_status",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_trace_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "display_title",
                table: "users");

            migrationBuilder.DropColumn(
                name: "locked_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "locked_by",
                table: "users");

            migrationBuilder.DropColumn(
                name: "locked_reason",
                table: "users");

            migrationBuilder.DropColumn(
                name: "website_url",
                table: "users");

            migrationBuilder.DropColumn(
                name: "new_value",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "old_value",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "trace_id",
                table: "audit_logs");

            migrationBuilder.RenameColumn(
                name: "password_changed_at",
                table: "users",
                newName: "PasswordChangedAt");

            migrationBuilder.RenameColumn(
                name: "PublishedByUserId",
                table: "posts",
                newName: "published_by_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_status",
                table: "users",
                sql: "status IN ('ACT','INACT','BAN','CLS')");

            migrationBuilder.CreateIndex(
                name: "IX_posts_published_by_user_id",
                table: "posts",
                column: "published_by_user_id");
        }
    }
}
