using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NewbieCoder.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_status",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "PasswordChangedAt",
                table: "users",
                newName: "password_changed_at");

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
                name: "explanation",
                table: "interview_questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "level_id",
                table: "interview_questions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "technology",
                table: "interview_questions",
                type: "character varying(100)",
                maxLength: 100,
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
                name: "levels",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_levels", x => x.id);
                    table.CheckConstraint("ck_levels_code", "code = upper(code)");
                    table.CheckConstraint("ck_levels_display_order", "display_order >= 0");
                    table.CheckConstraint("ck_levels_is_active", "is_active IN (true, false)");
                });

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
                name: "IX_interview_questions_level_id",
                table: "interview_questions",
                column: "level_id");

            migrationBuilder.CreateIndex(
                name: "IX_interview_questions_status_deleted_at",
                table: "interview_questions",
                columns: new[] { "status", "deleted_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_interview_questions_technology",
                table: "interview_questions",
                column: "technology");

            migrationBuilder.CreateIndex(
                name: "IX_interview_questions_topic",
                table: "interview_questions",
                column: "topic");

            migrationBuilder.CreateIndex(
                name: "IX_interview_question_tags_question_id_tag_id",
                table: "interview_question_tags",
                columns: new[] { "question_id", "tag_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_trace_id",
                table: "audit_logs",
                column: "trace_id");

            migrationBuilder.CreateIndex(
                name: "IX_levels_code",
                table: "levels",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_levels_deleted_at",
                table: "levels",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_levels_is_active",
                table: "levels",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_levels_name",
                table: "levels",
                column: "name");

            migrationBuilder.AddForeignKey(
                name: "FK_interview_questions_levels_level_id",
                table: "interview_questions",
                column: "level_id",
                principalTable: "levels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

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
                name: "FK_interview_questions_levels_level_id",
                table: "interview_questions");

            migrationBuilder.DropForeignKey(
                name: "FK_users_users_locked_by",
                table: "users");

            migrationBuilder.DropTable(
                name: "levels");

            migrationBuilder.DropTable(
                name: "seed_flags");

            migrationBuilder.DropIndex(
                name: "IX_users_locked_by",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_status",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_interview_questions_level_id",
                table: "interview_questions");

            migrationBuilder.DropIndex(
                name: "IX_interview_questions_status_deleted_at",
                table: "interview_questions");

            migrationBuilder.DropIndex(
                name: "IX_interview_questions_technology",
                table: "interview_questions");

            migrationBuilder.DropIndex(
                name: "IX_interview_questions_topic",
                table: "interview_questions");

            migrationBuilder.DropIndex(
                name: "IX_interview_question_tags_question_id_tag_id",
                table: "interview_question_tags");

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
                name: "explanation",
                table: "interview_questions");

            migrationBuilder.DropColumn(
                name: "level_id",
                table: "interview_questions");

            migrationBuilder.DropColumn(
                name: "technology",
                table: "interview_questions");

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

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_status",
                table: "users",
                sql: "status IN ('ACT','INACT','BAN','CLS')");
        }
    }
}
