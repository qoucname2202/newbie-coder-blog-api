using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewbieCoder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLockedUntilAndAddAuditStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Drop the legacy locked_until column on users
            migrationBuilder.DropIndex(
                name: "ix_users_locked_until_active",
                table: "users");

            migrationBuilder.DropColumn(
                name: "locked_until",
                table: "users");

            // 2) Add structured audit-log fields + index
            migrationBuilder.AddColumn<string>(
                name: "old_value",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "new_value",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trace_id",
                table: "audit_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_trace_id",
                table: "audit_logs",
                column: "trace_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the additions
            migrationBuilder.DropIndex(
                name: "IX_audit_logs_trace_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "trace_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "new_value",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "old_value",
                table: "audit_logs");

            // Restore locked_until + its partial index
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "locked_until",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_locked_until_active",
                table: "users",
                column: "locked_until",
                filter: "locked_until IS NOT NULL");
        }
    }
}
