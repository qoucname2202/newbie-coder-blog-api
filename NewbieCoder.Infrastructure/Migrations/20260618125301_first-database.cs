using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NewbieCoder.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class firstdatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "community_answers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
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
                    table.PrimaryKey("PK_community_answers", x => x.Id);
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
                });

            migrationBuilder.CreateTable(
                name: "community_questions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "OPEN"),
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
                    table.PrimaryKey("PK_community_questions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "interview_answers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
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
                    table.PrimaryKey("PK_interview_answers", x => x.Id);
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
                });

            migrationBuilder.CreateTable(
                name: "interview_questions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    author_id = table.Column<long>(type: "bigint", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    question_content = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    topic = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DRAFT"),
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
                    table.PrimaryKey("PK_interview_questions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    DeviceId = table.Column<long>(type: "bigint", nullable: true),
                    SessionId = table.Column<long>(type: "bigint", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    LoginStatus = table.Column<string>(type: "text", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    EffDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateLastMaint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RequestedIp = table.Column<string>(type: "text", nullable: true),
                    RequestedUserAgent = table.Column<string>(type: "text", nullable: true),
                    ExpiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EffDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateLastMaint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ACT"),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                    table.CheckConstraint("ck_permissions_action", "action IN ('VIEW','INIT','EDIT','DEL','APPR','RJCT','ASIG')");
                    table.CheckConstraint("ck_permissions_status", "status IN ('ACT','INACT')");
                });

            migrationBuilder.CreateTable(
                name: "post_categories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ACT"),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_categories", x => x.Id);
                    table.CheckConstraint("ck_post_cat_status", "status IN ('ACT','INACT')");
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
                });

            migrationBuilder.CreateTable(
                name: "posts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    thumbnail_url = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    visibility = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "PUBLIC"),
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
                    table.PrimaryKey("PK_posts", x => x.Id);
                    table.CheckConstraint("ck_posts_bookmark_cnt", "bookmark_count >= 0");
                    table.CheckConstraint("ck_posts_comment_cnt", "comment_count >= 0");
                    table.CheckConstraint("ck_posts_status", "status IN ('DRAFT','PENDING','PUB','RJCT','HIDDEN','DEL')");
                    table.CheckConstraint("ck_posts_view_cnt", "view_count >= 0");
                    table.CheckConstraint("ck_posts_visibility", "visibility IN ('PUBLIC','PRIVATE','LINK')");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    SessionId = table.Column<long>(type: "bigint", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    TokenFamily = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenId = table.Column<long>(type: "bigint", nullable: true),
                    EffDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateLastMaint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_id = table.Column<long>(type: "bigint", nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ACT"),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                    table.CheckConstraint("ck_roles_status", "status IN ('ACT','INACT')");
                });

            migrationBuilder.CreateTable(
                name: "series",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    thumbnail_url = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    visibility = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "PUBLIC"),
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
                    table.PrimaryKey("PK_series", x => x.Id);
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
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ACT"),
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
                    table.PrimaryKey("PK_tags", x => x.Id);
                    table.CheckConstraint("ck_tags_follow_cnt", "follower_count >= 0");
                    table.CheckConstraint("ck_tags_post_cnt", "post_count >= 0");
                    table.CheckConstraint("ck_tags_q_cnt", "question_count >= 0");
                    table.CheckConstraint("ck_tags_status", "status IN ('ACT','INACT')");
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    assigned_by = table.Column<long>(type: "bigint", nullable: true),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ACT"),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.Id);
                    table.CheckConstraint("ck_user_roles_status", "status IN ('ACT','RVKD','EXP')");
                });

            migrationBuilder.CreateTable(
                name: "UserDevices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: false),
                    DeviceName = table.Column<string>(type: "text", nullable: true),
                    DeviceType = table.Column<string>(type: "text", nullable: false),
                    Os = table.Column<string>(type: "text", nullable: true),
                    Browser = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    EffDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateLastMaint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    avatar_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "INACT"),
                    cover_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    github_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    linkedin_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_known_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    reputation_score = table.Column<int>(type: "integer", nullable: true),
                    follower_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    following_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    post_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    email_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    eff_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "date_trunc('day', now())"),
                    date_last_maint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.CheckConstraint("ck_users_follower_cnt", "follower_count >= 0");
                    table.CheckConstraint("ck_users_following_cnt", "following_count >= 0");
                    table.CheckConstraint("ck_users_password_len", "length(password) >= 60");
                    table.CheckConstraint("ck_users_post_cnt", "post_count >= 0");
                    table.CheckConstraint("ck_users_reputation", "reputation_score IS NULL OR reputation_score <= 100");
                    table.CheckConstraint("ck_users_status", "status IN ('ACT','INACT','BAN','CLS')");
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<long>(type: "bigint", nullable: true),
                    SessionTokenHash = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    LoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastActiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedReason = table.Column<string>(type: "text", nullable: true),
                    EffDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateLastMaint = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                });

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
                name: "IX_interview_questions_slug",
                table: "interview_questions",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_deleted_at",
                table: "permissions",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_module",
                table: "permissions",
                column: "module");

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
                name: "IX_role_permissions_deleted_at",
                table: "role_permissions",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_role_id_permission_id",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

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
                name: "IX_user_roles_deleted_at",
                table: "user_roles",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_user_id",
                table: "user_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_deleted_at",
                table: "users",
                column: "deleted_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "community_answers");

            migrationBuilder.DropTable(
                name: "community_question_tags");

            migrationBuilder.DropTable(
                name: "community_questions");

            migrationBuilder.DropTable(
                name: "interview_answers");

            migrationBuilder.DropTable(
                name: "interview_question_tags");

            migrationBuilder.DropTable(
                name: "interview_questions");

            migrationBuilder.DropTable(
                name: "LoginHistories");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "post_categories");

            migrationBuilder.DropTable(
                name: "post_tags");

            migrationBuilder.DropTable(
                name: "posts");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "series");

            migrationBuilder.DropTable(
                name: "series_posts");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "UserDevices");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "UserSessions");
        }
    }
}
