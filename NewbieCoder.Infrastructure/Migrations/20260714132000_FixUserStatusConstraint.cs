using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewbieCoder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixUserStatusConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Drop the old constraint without LOCKED
                    ALTER TABLE users DROP CONSTRAINT IF EXISTS ck_users_status;

                    -- Create new constraint with LOCKED included
                    ALTER TABLE users ADD CONSTRAINT ck_users_status
                        CHECK (status IN ('ACT','INACT','BAN','CLS','LOCKED'));
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE users DROP CONSTRAINT IF EXISTS ck_users_status;
                ALTER TABLE users ADD CONSTRAINT ck_users_status
                    CHECK (status IN ('ACT','INACT','BAN','CLS'));
            ");
        }
    }
}
