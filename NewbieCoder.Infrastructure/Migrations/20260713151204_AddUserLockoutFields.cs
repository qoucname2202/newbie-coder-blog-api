using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NewbieCoder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLockoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "post_categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_categories", x => x.id);
                    table.CheckConstraint("ck_post_cat_status", "status IN ('active','inactive')");
                    table.ForeignKey(
                        name: "FK_post_categories_post_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "post_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.CheckConstraint("ck_roles_status", "status IN ('active','inactive')");
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    post_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    question_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    follower_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                    table.CheckConstraint("ck_tags_follow_cnt", "follower_count >= 0");
                    table.CheckConstraint("ck_tags_post_cnt", "post_count >= 0");
                    table.CheckConstraint("ck_tags_q_cnt", "question_count >= 0");
                    table.CheckConstraint("ck_tags_status", "status IN ('active','inactive')");
                });

            // NOTE: The users table already exists from previous migrations.
            // We need to ALTER it instead of CREATE it.
            // Execute raw SQL to add missing columns and update constraint
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Add lockout columns if they don't exist
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'locked_at') THEN
                        ALTER TABLE users ADD COLUMN locked_at TIMESTAMP WITH TIME ZONE NULL;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'locked_until') THEN
                        ALTER TABLE users ADD COLUMN locked_until TIMESTAMP WITH TIME ZONE NULL;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'locked_reason') THEN
                        ALTER TABLE users ADD COLUMN locked_reason VARCHAR(500) NULL;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'users' AND column_name = 'locked_by') THEN
                        ALTER TABLE users ADD COLUMN locked_by BIGINT NULL;
                    END IF;

                    -- Add indexes for lockout management
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'users' AND indexname = 'ix_users_locked_until_active') THEN
                        CREATE INDEX ix_users_locked_until_active ON users (locked_until) WHERE locked_until IS NOT NULL;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'users' AND indexname = 'ix_users_locked_by') THEN
                        CREATE INDEX ix_users_locked_by ON users (locked_by);
                    END IF;

                    -- Update check constraint to include LOCKED status
                    -- Drop and recreate the constraint
                    ALTER TABLE users DROP CONSTRAINT IF EXISTS ck_users_status;
                    ALTER TABLE users ADD CONSTRAINT ck_users_status CHECK (status IN ('ACT','INACT','BAN','CLS','LOCKED'));

                    -- Add FK for locked_by (skip if already exists)
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.table_constraints
                        WHERE constraint_name = 'fk_users_locked_by'
                          AND table_name = 'users'
                    ) THEN
                        ALTER TABLE users ADD CONSTRAINT fk_users_locked_by FOREIGN KEY (locked_by) REFERENCES users(id) ON DELETE NO ACTION;
                    END IF;
                END $$;
            ");

            migrationBuilder.CreateTable(
                name: "interview_questions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    author_id = table.Column<long>(type: "bigint", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    question_content = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<string>(type: "text", nullable: false),
                    topic = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "draft"),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    vote_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bookmark_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interview_questions", x => x.id);
                    table.CheckConstraint("ck_iq_level", "level IN ('entry','junior','middle','senior','expert')");
                    table.CheckConstraint("ck_iq_status", "status IN ('draft','pending','published','rejected','hidden','deleted')");
                    table.ForeignKey(
                        name: "FK_interview_questions_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    requested_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    requested_user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_tokens", x => x.id);
                    table.CheckConstraint("ck_prt_status", "status IN ('active','used','expired','revoked')");
                    table.ForeignKey(
                        name: "FK_password_reset_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "posts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    thumbnail_url = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "draft"),
                    visibility = table.Column<string>(type: "text", nullable: false, defaultValue: "public"),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    comment_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    vote_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bookmark_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_posts", x => x.id);
                    table.CheckConstraint("ck_posts_bookmark_cnt", "bookmark_count >= 0");
                    table.CheckConstraint("ck_posts_comment_cnt", "comment_count >= 0");
                    table.CheckConstraint("ck_posts_status", "status IN ('draft','pending','published','rejected','hidden','deleted')");
                    table.CheckConstraint("ck_posts_view_cnt", "view_count >= 0");
                    table.CheckConstraint("ck_posts_visibility", "visibility IN ('public','private','link_only')");
                    table.ForeignKey(
                        name: "FK_posts_post_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "post_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_posts_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "series",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    thumbnail_url = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "draft"),
                    visibility = table.Column<string>(type: "text", nullable: false, defaultValue: "public"),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bookmark_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_series", x => x.id);
                    table.CheckConstraint("ck_series_status", "status IN ('draft','pending','published','rejected','hidden','deleted')");
                    table.CheckConstraint("ck_series_visibility", "visibility IN ('public','private','link_only')");
                    table.ForeignKey(
                        name: "FK_series_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_devices",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    device_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    device_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    device_type = table.Column<string>(type: "text", nullable: false, defaultValue: "web"),
                    os = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    browser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_devices", x => x.id);
                    table.CheckConstraint("ck_user_devices_status", "status IN ('active','revoked','blocked')");
                    table.CheckConstraint("ck_user_devices_type", "device_type IN ('web','mobile','tablet','desktop')");
                    table.ForeignKey(
                        name: "FK_user_devices_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    assigned_by = table.Column<long>(type: "bigint", nullable: true),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.id);
                    table.CheckConstraint("ck_user_roles_status", "status IN ('active','revoked','expired')");
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_assigned_by",
                        column: x => x.assigned_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interview_answers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    question_id = table.Column<long>(type: "bigint", nullable: false),
                    answer_content = table.Column<string>(type: "text", nullable: false),
                    explanation = table.Column<string>(type: "text", nullable: true),
                    example = table.Column<string>(type: "text", nullable: true),
                    is_official = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interview_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_interview_answers_interview_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "interview_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "interview_question_tags",
                columns: table => new
                {
                    question_id = table.Column<long>(type: "bigint", nullable: false),
                    tag_id = table.Column<long>(type: "bigint", nullable: false),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interview_question_tags", x => new { x.question_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_interview_question_tags_interview_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "interview_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_interview_question_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "post_tags",
                columns: table => new
                {
                    post_id = table.Column<long>(type: "bigint", nullable: false),
                    tag_id = table.Column<long>(type: "bigint", nullable: false),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_tags", x => new { x.post_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_post_tags_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_post_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "series_posts",
                columns: table => new
                {
                    series_id = table.Column<long>(type: "bigint", nullable: false),
                    post_id = table.Column<long>(type: "bigint", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_series_posts", x => new { x.series_id, x.post_id });
                    table.ForeignKey(
                        name: "FK_series_posts_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_series_posts_series_series_id",
                        column: x => x.series_id,
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    device_id = table.Column<long>(type: "bigint", nullable: true),
                    session_token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_active_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.id);
                    table.CheckConstraint("ck_user_sessions_status", "status IN ('active','expired','revoked')");
                    table.ForeignKey(
                        name: "FK_user_sessions_user_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "user_devices",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    session_id = table.Column<long>(type: "bigint", nullable: true),
                    device_id = table.Column<long>(type: "bigint", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<long>(type: "bigint", nullable: true),
                    details = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_user_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "user_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_audit_logs_user_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "user_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "login_histories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    device_id = table.Column<long>(type: "bigint", nullable: true),
                    session_id = table.Column<long>(type: "bigint", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    login_status = table.Column<string>(type: "text", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_histories", x => x.id);
                    table.CheckConstraint("ck_login_histories_status", "login_status IN ('success','failed')");
                    table.ForeignKey(
                        name: "FK_login_histories_user_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "user_devices",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_login_histories_user_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "user_sessions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_login_histories_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    session_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    token_family = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "active"),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_id = table.Column<long>(type: "bigint", nullable: true),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.CheckConstraint("ck_refresh_tokens_status", "status IN ('active','used','revoked','expired')");
                    table.ForeignKey(
                        name: "FK_refresh_tokens_refresh_tokens_replaced_by_token_id",
                        column: x => x.replaced_by_token_id,
                        principalTable: "refresh_tokens",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_refresh_tokens_user_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "user_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "community_answers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    question_id = table.Column<long>(type: "bigint", nullable: false),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    vote_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_accepted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_community_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_community_answers_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "community_questions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "open"),
                    accepted_answer_id = table.Column<long>(type: "bigint", nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    answer_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    vote_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bookmark_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_community_questions", x => x.id);
                    table.CheckConstraint("ck_cq_status", "status IN ('open','answered','resolved','closed','hidden')");
                    table.ForeignKey(
                        name: "FK_community_questions_community_answers_accepted_answer_id",
                        column: x => x.accepted_answer_id,
                        principalTable: "community_answers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_community_questions_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "community_question_tags",
                columns: table => new
                {
                    question_id = table.Column<long>(type: "bigint", nullable: false),
                    tag_id = table.Column<long>(type: "bigint", nullable: false),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_community_question_tags", x => new { x.question_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_community_question_tags_community_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "community_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_community_question_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_device_id",
                table: "audit_logs",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_session_id",
                table: "audit_logs",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_community_answers_author_id",
                table: "community_answers",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_community_answers_deleted_at",
                table: "community_answers",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_community_answers_question_id",
                table: "community_answers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_community_question_tags_tag_id",
                table: "community_question_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_community_questions_accepted_answer_id",
                table: "community_questions",
                column: "accepted_answer_id");

            migrationBuilder.CreateIndex(
                name: "IX_community_questions_author_id",
                table: "community_questions",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_community_questions_deleted_at",
                table: "community_questions",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_community_questions_slug",
                table: "community_questions",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_community_questions_status",
                table: "community_questions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_interview_answers_deleted_at",
                table: "interview_answers",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_interview_answers_question_id",
                table: "interview_answers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_interview_question_tags_tag_id",
                table: "interview_question_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_interview_questions_author_id",
                table: "interview_questions",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_interview_questions_deleted_at",
                table: "interview_questions",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_interview_questions_level",
                table: "interview_questions",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "IX_interview_questions_slug",
                table: "interview_questions",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_interview_questions_status",
                table: "interview_questions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_login_histories_deleted_at",
                table: "login_histories",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_login_histories_device_id",
                table: "login_histories",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_login_histories_eff_date",
                table: "login_histories",
                column: "eff_date");

            migrationBuilder.CreateIndex(
                name: "IX_login_histories_login_status",
                table: "login_histories",
                column: "login_status");

            migrationBuilder.CreateIndex(
                name: "IX_login_histories_session_id",
                table: "login_histories",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_login_histories_user_id",
                table: "login_histories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_deleted_at",
                table: "password_reset_tokens",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_status",
                table: "password_reset_tokens",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_token_hash",
                table: "password_reset_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_user_id",
                table: "password_reset_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_post_categories_deleted_at",
                table: "post_categories",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_post_categories_parent_id",
                table: "post_categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_post_categories_slug",
                table: "post_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_post_tags_tag_id",
                table: "post_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_posts_author_id",
                table: "posts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_posts_category_id",
                table: "posts",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_posts_deleted_at",
                table: "posts",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_posts_slug",
                table: "posts",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_posts_status",
                table: "posts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_deleted_at",
                table: "refresh_tokens",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_replaced_by_token_id",
                table: "refresh_tokens",
                column: "replaced_by_token_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_session_id",
                table: "refresh_tokens",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_status",
                table: "refresh_tokens",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_family",
                table: "refresh_tokens",
                column: "token_family");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_deleted_at",
                table: "roles",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_roles_status",
                table: "roles",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_series_author_id",
                table: "series",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_series_deleted_at",
                table: "series",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_series_slug",
                table: "series",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_series_status",
                table: "series",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_series_posts_post_id",
                table: "series_posts",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_series_posts_series_id_position",
                table: "series_posts",
                columns: new[] { "series_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tags_deleted_at",
                table: "tags",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tags_slug",
                table: "tags",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tags_status",
                table: "tags",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_user_devices_deleted_at",
                table: "user_devices",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_devices_status",
                table: "user_devices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_user_devices_user_id",
                table: "user_devices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_devices_user_id_device_id",
                table: "user_devices",
                columns: new[] { "user_id", "device_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_assigned_by",
                table: "user_roles",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_deleted_at",
                table: "user_roles",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_status",
                table: "user_roles",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_user_id",
                table: "user_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_deleted_at",
                table: "user_sessions",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_device_id",
                table: "user_sessions",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_expired_at",
                table: "user_sessions",
                column: "expired_at");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_status",
                table: "user_sessions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_user_id",
                table: "user_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_deleted_at",
                table: "users",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_deleted_by",
                table: "users",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_locked_by",
                table: "users",
                column: "locked_by");

            migrationBuilder.CreateIndex(
                name: "ix_users_locked_until_active",
                table: "users",
                column: "locked_until",
                filter: "locked_until IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_status",
                table: "users",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_community_answers_community_questions_question_id",
                table: "community_answers",
                column: "question_id",
                principalTable: "community_questions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_community_answers_users_author_id",
                table: "community_answers");

            migrationBuilder.DropForeignKey(
                name: "FK_community_questions_users_author_id",
                table: "community_questions");

            migrationBuilder.DropForeignKey(
                name: "FK_community_answers_community_questions_question_id",
                table: "community_answers");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "community_question_tags");

            migrationBuilder.DropTable(
                name: "interview_answers");

            migrationBuilder.DropTable(
                name: "interview_question_tags");

            migrationBuilder.DropTable(
                name: "login_histories");

            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropTable(
                name: "post_tags");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "series_posts");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "interview_questions");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "user_sessions");

            migrationBuilder.DropTable(
                name: "posts");

            migrationBuilder.DropTable(
                name: "series");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "user_devices");

            migrationBuilder.DropTable(
                name: "post_categories");

            // NOTE: The users table must NOT be dropped in Down() because it
            // existed before this migration. Instead, we revert the constraint.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Restore the original constraint without LOCKED
                    ALTER TABLE users DROP CONSTRAINT IF EXISTS ck_users_status;
                    ALTER TABLE users ADD CONSTRAINT ck_users_status CHECK (status IN ('ACT','INACT','BAN','CLS'));

                    -- Optionally remove the added columns (uncomment if needed for strict rollback)
                    -- ALTER TABLE users DROP COLUMN IF EXISTS locked_by;
                    -- ALTER TABLE users DROP COLUMN IF EXISTS locked_reason;
                    -- ALTER TABLE users DROP COLUMN IF EXISTS locked_until;
                    -- ALTER TABLE users DROP COLUMN IF EXISTS locked_at;
                    -- DROP INDEX IF EXISTS ix_users_locked_by;
                    -- DROP INDEX IF EXISTS ix_users_locked_until_active;
                END $$;
            ");

            migrationBuilder.DropTable(
                name: "community_questions");

            migrationBuilder.DropTable(
                name: "community_answers");
        }
    }
}
