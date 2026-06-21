using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewbieCoder.Core.Entities;

namespace NewbieCoder.Infrastructure.Persistence.Configurations;

internal static class PgConfig
{
    public static void Base<TEntity>(EntityTypeBuilder<TEntity> b) where TEntity : BaseEntity
    {
        // id BIGINT GENERATED ALWAYS AS IDENTITY -> DB generates it, EF must not send a value on insert
        b.HasKey(x => x.Id);
        b.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityAlwaysColumn()
            .ValueGeneratedOnAdd();

        // eff_date: set once on INSERT, never updated afterwards
        b.Property(x => x.EffDate)
            .HasColumnName("eff_date")
            .HasDefaultValueSql("date_trunc('day', now())")
            .ValueGeneratedOnAdd();

        // date_last_maint: set on INSERT, then auto-updated by a DB trigger on every UPDATE
        // -> ValueGeneratedOnAddOrUpdate() so EF always re-reads the DB value instead of sending its own
        b.Property(x => x.DateLastMaint)
            .HasColumnName("date_last_maint")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        b.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        b.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        b.HasIndex(x => x.DeletedAt).HasFilter("deleted_at IS NULL");
    }
}

// ============================================================
// 1. users
// ============================================================
public class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        PgConfig.Base(b);

        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(100).IsRequired();
        b.Property(x => x.Password).HasColumnName("password").HasMaxLength(255).IsRequired();
        b.Property(x => x.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
        b.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(150).IsRequired();
        b.Property(x => x.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(1000);
        b.Property(x => x.Bio).HasColumnName("bio").HasMaxLength(500);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(10).HasDefaultValue("INACT");
        b.Property(x => x.CoverUrl).HasColumnName("cover_url").HasMaxLength(1000);
        b.Property(x => x.GithubUrl).HasColumnName("github_url").HasMaxLength(500);
        b.Property(x => x.LinkedinUrl).HasColumnName("linkedin_url").HasMaxLength(500);

        // SQL column is named "location", VARCHAR(50) NOT NULL (comment: "IP address of the user")
        // -> renamed from "LastKnownIp/last_known_ip" (didn't match the SQL column) to "Location/location"
        b.Property(x => x.Location).HasColumnName("location").HasMaxLength(50).IsRequired();

        b.Property(x => x.ReputationScore).HasColumnName("reputation_score");
        b.Property(x => x.FollowerCount).HasColumnName("follower_count").HasDefaultValue(0L);
        b.Property(x => x.FollowingCount).HasColumnName("following_count").HasDefaultValue(0L);
        b.Property(x => x.PostCount).HasColumnName("post_count").HasDefaultValue(0);
        b.Property(x => x.EmailVerified).HasColumnName("email_verified").HasDefaultValue(false);
        b.Property(x => x.EmailVerifiedAt).HasColumnName("email_verified_at");
        b.Property(x => x.LastLoginAt).HasColumnName("last_login_at");

        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.Username).IsUnique();
        b.HasIndex(x => x.Status);

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_users_status", "status IN ('ACT','INACT','BAN','CLS')");
            t.HasCheckConstraint("ck_users_reputation", "reputation_score IS NULL OR reputation_score <= 100");
            t.HasCheckConstraint("ck_users_password_len", "length(password) >= 60");
            t.HasCheckConstraint("ck_users_follower_cnt", "follower_count >= 0");
            t.HasCheckConstraint("ck_users_following_cnt", "following_count >= 0");
            t.HasCheckConstraint("ck_users_post_cnt", "post_count >= 0");
        });

        // self-FK: deleted_by -> users.id
        b.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.DeletedBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

// ============================================================
// 2. roles
// ============================================================
public class RoleConfig : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");
        PgConfig.Base(b);
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
        b.Property(x => x.IsSystem).HasColumnName("is_system").HasDefaultValue(false);

        // SQL: VARCHAR(20), values 'active'/'inactive' (NOT the abbreviated 'ACT'/'INACT')
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active");

        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.Status);

        b.ToTable(t => t.HasCheckConstraint("ck_roles_status", "status IN ('active','inactive')"));
    }
}

// ============================================================
// 3. permissions
// ============================================================
public class PermissionConfig : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("permissions");
        PgConfig.Base(b);
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        b.Property(x => x.Module).HasColumnName("module").HasMaxLength(100).IsRequired();

        // SQL: VARCHAR(50), not VARCHAR(10)
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(50).IsRequired();

        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("ACT");

        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.Module);
        b.HasIndex(x => x.Status);

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_permissions_action", "action IN ('VIEW','INIT','EDIT','DEL','APPR','RJCT','ASIG')");
            t.HasCheckConstraint("ck_permissions_status", "status IN ('ACT','INACT')");
        });
    }
}

// ============================================================
// 4. user_roles
// ============================================================
public class UserRoleConfig : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.ToTable("user_roles");
        PgConfig.Base(b);
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.RoleId).HasColumnName("role_id");
        b.Property(x => x.AssignedBy).HasColumnName("assigned_by");
        b.Property(x => x.AssignedAt).HasColumnName("assigned_at").HasDefaultValueSql("now()");
        b.Property(x => x.ExpiredAt).HasColumnName("expired_at");

        // SQL: VARCHAR(20), values 'active'/'revoked'/'expired'
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active");

        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.RoleId);
        b.HasIndex(x => x.Status);

        b.ToTable(t => t.HasCheckConstraint("ck_user_roles_status", "status IN ('active','revoked','expired')"));

        b.HasOne(x => x.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK previously missing: assigned_by -> users.id
        b.HasOne(x => x.AssignedByUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

// ============================================================
// 5. role_permissions
// ============================================================
public class RolePermissionConfig : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("role_permissions");
        PgConfig.Base(b);
        b.Property(x => x.RoleId).HasColumnName("role_id");
        b.Property(x => x.PermissionId).HasColumnName("permission_id");
        b.Property(x => x.CreatedBy).HasColumnName("created_by");

        b.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        b.HasIndex(x => x.PermissionId);

        b.HasOne(x => x.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK previously missing: created_by -> users.id
        b.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

// ============================================================
// 6. user_devices  (MISSING FROM THE ORIGINAL FILE — newly added)
// ============================================================
public class UserDeviceConfig : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> b)
    {
        b.ToTable("user_devices");
        PgConfig.Base(b);

        b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        b.Property(x => x.DeviceId).HasColumnName("device_id").HasMaxLength(100).IsRequired();
        b.Property(x => x.DeviceName).HasColumnName("device_name").HasMaxLength(150);
        b.Property(x => x.DeviceType).HasColumnName("device_type").HasMaxLength(30).IsRequired();
        b.Property(x => x.Os).HasColumnName("os").HasMaxLength(100);
        b.Property(x => x.Browser).HasColumnName("browser").HasMaxLength(100);
        b.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        b.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        b.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("active");

        b.HasIndex(x => new { x.UserId, x.DeviceId }).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Status);

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_user_devices_type", "device_type IN ('web','mobile','tablet','desktop')");
            t.HasCheckConstraint("ck_user_devices_status", "status IN ('active','revoked','blocked')");
        });

        b.HasOne(x => x.User)
            .WithMany(u => u.Devices)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ============================================================
// 7. user_sessions  (MISSING FROM THE ORIGINAL FILE — newly added)
// ============================================================
public class UserSessionConfig : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> b)
    {
        b.ToTable("user_sessions");
        PgConfig.Base(b);

        b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        b.Property(x => x.DeviceId).HasColumnName("device_id");
        b.Property(x => x.SessionTokenHash).HasColumnName("session_token_hash").HasMaxLength(255).IsRequired();
        b.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        b.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("active");
        b.Property(x => x.LoginAt).HasColumnName("login_at").HasDefaultValueSql("now()");
        b.Property(x => x.LastActiveAt).HasColumnName("last_active_at");
        b.Property(x => x.ExpiredAt).HasColumnName("expired_at").IsRequired();
        b.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        b.Property(x => x.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(255);

        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ExpiredAt);

        b.ToTable(t => t.HasCheckConstraint("ck_user_sessions_status", "status IN ('active','expired','revoked')"));

        b.HasOne(x => x.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Device)
            .WithMany(d => d.Sessions)
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

// ============================================================
// 8. refresh_tokens  (MISSING FROM THE ORIGINAL FILE — newly added)
// ============================================================
public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        PgConfig.Base(b);

        b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        b.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        b.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        b.Property(x => x.TokenFamily).HasColumnName("token_family").HasColumnType("uuid");
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("active");
        b.Property(x => x.IssuedAt).HasColumnName("issued_at").HasDefaultValueSql("now()");
        b.Property(x => x.ExpiredAt).HasColumnName("expired_at").IsRequired();
        b.Property(x => x.UsedAt).HasColumnName("used_at");
        b.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        b.Property(x => x.ReplacedByTokenId).HasColumnName("replaced_by_token_id");

        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.SessionId);
        b.HasIndex(x => x.TokenFamily);
        b.HasIndex(x => x.Status);

        b.ToTable(t => t.HasCheckConstraint("ck_refresh_tokens_status", "status IN ('active','used','revoked','expired')"));

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Session)
            .WithMany(s => s.RefreshTokens)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // self-FK: replaced_by_token_id -> refresh_tokens.id
        b.HasOne(x => x.ReplacedByToken)
            .WithMany()
            .HasForeignKey(x => x.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

// ============================================================
// 9. login_histories  (MISSING FROM THE ORIGINAL FILE — newly added)
// ============================================================
public class LoginHistoryConfig : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(EntityTypeBuilder<LoginHistory> b)
    {
        b.ToTable("login_histories");
        PgConfig.Base(b);

        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
        b.Property(x => x.DeviceId).HasColumnName("device_id");
        b.Property(x => x.SessionId).HasColumnName("session_id");
        b.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        b.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        b.Property(x => x.LoginStatus).HasColumnName("login_status").HasMaxLength(30).IsRequired();
        b.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(100);

        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.LoginStatus);
        b.HasIndex(x => x.EffDate);

        b.ToTable(t => t.HasCheckConstraint("ck_login_histories_status", "login_status IN ('success','failed')"));

        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.NoAction);
    }
}

// ============================================================
// 10. password_reset_tokens  (MISSING FROM THE ORIGINAL FILE — newly added)
// ============================================================
public class PasswordResetTokenConfig : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> b)
    {
        b.ToTable("password_reset_tokens");
        PgConfig.Base(b);

        b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        b.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("active");
        b.Property(x => x.RequestedIp).HasColumnName("requested_ip").HasMaxLength(45);
        b.Property(x => x.RequestedUserAgent).HasColumnName("requested_user_agent").HasMaxLength(500);
        b.Property(x => x.ExpiredAt).HasColumnName("expired_at").IsRequired();
        b.Property(x => x.UsedAt).HasColumnName("used_at");
        b.Property(x => x.RevokedAt).HasColumnName("revoked_at");

        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Status);

        b.ToTable(t => t.HasCheckConstraint("ck_prt_status", "status IN ('active','used','expired','revoked')"));

        // Original SQL: ON DELETE CASCADE
        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ============================================================
// 11. post_categories
// ============================================================
public class PostCategoryConfig : IEntityTypeConfiguration<PostCategory>
{
    public void Configure(EntityTypeBuilder<PostCategory> b)
    {
        b.ToTable("post_categories");
        PgConfig.Base(b);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(255).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        b.Property(x => x.ParentId).HasColumnName("parent_id");

        // SQL: VARCHAR(30), values 'active'/'inactive'
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("active");

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.ParentId);

        b.ToTable(t => t.HasCheckConstraint("ck_post_cat_status", "status IN ('active','inactive')"));

        // self-FK: parent_id -> post_categories.id, ON DELETE SET NULL
        b.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

// ============================================================
// 12. tags
// ============================================================
public class TagConfig : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.ToTable("tags");
        PgConfig.Base(b);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasColumnType("text");

        // SQL: VARCHAR(30)
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("active");

        b.Property(x => x.PostCount).HasColumnName("post_count").HasDefaultValue(0);
        b.Property(x => x.QuestionCount).HasColumnName("question_count").HasDefaultValue(0);
        b.Property(x => x.FollowerCount).HasColumnName("follower_count").HasDefaultValue(0);

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.Status);

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_tags_status", "status IN ('active','inactive')");
            t.HasCheckConstraint("ck_tags_post_cnt", "post_count >= 0");
            t.HasCheckConstraint("ck_tags_q_cnt", "question_count >= 0");
            t.HasCheckConstraint("ck_tags_follow_cnt", "follower_count >= 0");
        });
    }
}

// ============================================================
// 13. posts
// ============================================================
public class PostConfig : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> b)
    {
        b.ToTable("posts");
        PgConfig.Base(b);
        b.Property(x => x.AuthorId).HasColumnName("author_id").IsRequired();
        b.Property(x => x.CategoryId).HasColumnName("category_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(500).IsRequired();
        b.Property(x => x.Summary).HasColumnName("summary").HasColumnType("text");
        b.Property(x => x.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        b.Property(x => x.ThumbnailUrl).HasColumnName("thumbnail_url").HasColumnType("text");

        // SQL: VARCHAR(30), values 'draft','pending','published','rejected','hidden','deleted'
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("draft");

        // SQL: VARCHAR(30), values 'public','private','link_only'
        b.Property(x => x.Visibility).HasColumnName("visibility").HasMaxLength(30).HasDefaultValue("public");

        b.Property(x => x.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
        b.Property(x => x.CommentCount).HasColumnName("comment_count").HasDefaultValue(0);
        b.Property(x => x.VoteScore).HasColumnName("vote_score").HasDefaultValue(0);
        b.Property(x => x.BookmarkCount).HasColumnName("bookmark_count").HasDefaultValue(0);
        b.Property(x => x.PublishedAt).HasColumnName("published_at");

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.AuthorId);
        b.HasIndex(x => x.CategoryId);
        b.HasIndex(x => x.Status);

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_posts_status", "status IN ('draft','pending','published','rejected','hidden','deleted')");
            t.HasCheckConstraint("ck_posts_visibility", "visibility IN ('public','private','link_only')");
            t.HasCheckConstraint("ck_posts_view_cnt", "view_count >= 0");
            t.HasCheckConstraint("ck_posts_comment_cnt", "comment_count >= 0");
            t.HasCheckConstraint("ck_posts_bookmark_cnt", "bookmark_count >= 0");
        });

        b.HasOne(x => x.Author)
            .WithMany(u => u.Posts)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Category)
            .WithMany(c => c.Posts)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

// ============================================================
// 14. post_tags
// ============================================================
public class PostTagConfig : IEntityTypeConfiguration<PostTag>
{
    public void Configure(EntityTypeBuilder<PostTag> b)
    {
        b.ToTable("post_tags");
        b.HasKey(x => new { x.PostId, x.TagId });
        b.Property(x => x.PostId).HasColumnName("post_id");
        b.Property(x => x.TagId).HasColumnName("tag_id");
        b.Property(x => x.EffDate)
            .HasColumnName("eff_date")
            .HasDefaultValueSql("date_trunc('day', now())")
            .ValueGeneratedOnAdd();
        b.HasIndex(x => x.TagId);

        b.HasOne(x => x.Post)
            .WithMany(p => p.PostTags)
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Tag)
            .WithMany(t => t.PostTags)
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ============================================================
// 15. series
// ============================================================
public class SeriesConfig : IEntityTypeConfiguration<Series>
{
    public void Configure(EntityTypeBuilder<Series> b)
    {
        b.ToTable("series");
        PgConfig.Base(b);
        b.Property(x => x.AuthorId).HasColumnName("author_id").IsRequired();
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(500).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        b.Property(x => x.ThumbnailUrl).HasColumnName("thumbnail_url").HasColumnType("text");

        // SQL: VARCHAR(30)
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("draft");
        b.Property(x => x.Visibility).HasColumnName("visibility").HasMaxLength(30).HasDefaultValue("public");

        b.Property(x => x.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
        b.Property(x => x.BookmarkCount).HasColumnName("bookmark_count").HasDefaultValue(0);
        b.Property(x => x.PublishedAt).HasColumnName("published_at");

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.AuthorId);
        b.HasIndex(x => x.Status);

        // CHECK constraint missing from the original — added here
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_series_status", "status IN ('draft','pending','published','rejected','hidden','deleted')");
            t.HasCheckConstraint("ck_series_visibility", "visibility IN ('public','private','link_only')");
        });

        b.HasOne(x => x.Author)
            .WithMany(u => u.Series)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ============================================================
// 16. series_posts
// ============================================================
public class SeriesPostConfig : IEntityTypeConfiguration<SeriesPost>
{
    public void Configure(EntityTypeBuilder<SeriesPost> b)
    {
        b.ToTable("series_posts");
        b.HasKey(x => new { x.SeriesId, x.PostId });
        b.Property(x => x.SeriesId).HasColumnName("series_id");
        b.Property(x => x.PostId).HasColumnName("post_id");
        b.Property(x => x.Position).HasColumnName("position").HasDefaultValue(1);
        b.Property(x => x.EffDate)
            .HasColumnName("eff_date")
            .HasDefaultValueSql("date_trunc('day', now())")
            .ValueGeneratedOnAdd();

        b.HasIndex(x => new { x.SeriesId, x.Position }).IsUnique();
        b.HasIndex(x => x.PostId);

        b.HasOne(x => x.Series)
            .WithMany(s => s.SeriesPosts)
            .HasForeignKey(x => x.SeriesId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Post)
            .WithMany(p => p.SeriesPosts)
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ============================================================
// 17. interview_questions
// ============================================================
public class InterviewQuestionConfig : IEntityTypeConfiguration<InterviewQuestion>
{
    public void Configure(EntityTypeBuilder<InterviewQuestion> b)
    {
        b.ToTable("interview_questions");
        PgConfig.Base(b);
        b.Property(x => x.AuthorId).HasColumnName("author_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(500).IsRequired();
        b.Property(x => x.QuestionContent).HasColumnName("question_content").HasColumnType("text").IsRequired();

        // SQL: VARCHAR(30), values 'entry','junior','middle','senior','expert'
        b.Property(x => x.Level).HasColumnName("level").HasMaxLength(30).IsRequired();

        b.Property(x => x.Topic).HasColumnName("topic").HasMaxLength(100);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("draft");
        b.Property(x => x.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
        b.Property(x => x.VoteScore).HasColumnName("vote_score").HasDefaultValue(0);
        b.Property(x => x.BookmarkCount).HasColumnName("bookmark_count").HasDefaultValue(0);
        b.Property(x => x.PublishedAt).HasColumnName("published_at");

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.AuthorId);
        b.HasIndex(x => x.Level);
        b.HasIndex(x => x.Status);

        // CHECK constraint missing from the original — added here
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_iq_level", "level IN ('entry','junior','middle','senior','expert')");
            t.HasCheckConstraint("ck_iq_status", "status IN ('draft','pending','published','rejected','hidden','deleted')");
        });

        // SQL: ON DELETE SET NULL
        b.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

// ============================================================
// 18. interview_answers
// ============================================================
public class InterviewAnswerConfig : IEntityTypeConfiguration<InterviewAnswer>
{
    public void Configure(EntityTypeBuilder<InterviewAnswer> b)
    {
        b.ToTable("interview_answers");
        PgConfig.Base(b);
        b.Property(x => x.QuestionId).HasColumnName("question_id").IsRequired();
        b.Property(x => x.AnswerContent).HasColumnName("answer_content").HasColumnType("text").IsRequired();
        b.Property(x => x.Explanation).HasColumnName("explanation").HasColumnType("text");
        b.Property(x => x.Example).HasColumnName("example").HasColumnType("text");
        b.Property(x => x.IsOfficial).HasColumnName("is_official").HasDefaultValue(true);

        b.HasIndex(x => x.QuestionId);

        b.HasOne(x => x.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ============================================================
// 19. interview_question_tags
// ============================================================
public class InterviewQuestionTagConfig : IEntityTypeConfiguration<InterviewQuestionTag>
{
    public void Configure(EntityTypeBuilder<InterviewQuestionTag> b)
    {
        b.ToTable("interview_question_tags");
        b.HasKey(x => new { x.QuestionId, x.TagId });
        b.Property(x => x.QuestionId).HasColumnName("question_id");
        b.Property(x => x.TagId).HasColumnName("tag_id");
        b.Property(x => x.EffDate)
            .HasColumnName("eff_date")
            .HasDefaultValueSql("date_trunc('day', now())")
            .ValueGeneratedOnAdd();
        b.HasIndex(x => x.TagId);

        b.HasOne(x => x.Question)
            .WithMany(q => q.InterviewQuestionTags)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Tag)
            .WithMany(t => t.InterviewQuestionTags)
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ============================================================
// 20. community_questions
// ============================================================
public class CommunityQuestionConfig : IEntityTypeConfiguration<CommunityQuestion>
{
    public void Configure(EntityTypeBuilder<CommunityQuestion> b)
    {
        b.ToTable("community_questions");
        PgConfig.Base(b);
        b.Property(x => x.AuthorId).HasColumnName("author_id").IsRequired();
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(500).IsRequired();
        b.Property(x => x.Content).HasColumnName("content").HasColumnType("text").IsRequired();

        // SQL: VARCHAR(30), values 'open','answered','resolved','closed','hidden'
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("open");

        b.Property(x => x.AcceptedAnswerId).HasColumnName("accepted_answer_id");
        b.Property(x => x.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
        b.Property(x => x.AnswerCount).HasColumnName("answer_count").HasDefaultValue(0);
        b.Property(x => x.VoteScore).HasColumnName("vote_score").HasDefaultValue(0);
        b.Property(x => x.BookmarkCount).HasColumnName("bookmark_count").HasDefaultValue(0);
        b.Property(x => x.ClosedAt).HasColumnName("closed_at");

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.AuthorId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.AcceptedAnswerId);

        // CHECK constraint missing from the original — added here
        b.ToTable(t => t.HasCheckConstraint("ck_cq_status", "status IN ('open','answered','resolved','closed','hidden')"));

        b.HasOne(x => x.Author)
            .WithMany(u => u.CommunityQuestions)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK missing from the original: accepted_answer_id -> community_answers.id
        // In the SQL this FK is added via ALTER TABLE after community_answers exists
        // (to avoid a circular dependency at CREATE TABLE time) — EF handles the
        // migration ordering automatically.
        b.HasOne(x => x.AcceptedAnswer)
            .WithMany()
            .HasForeignKey(x => x.AcceptedAnswerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

// ============================================================
// 21. community_answers
// ============================================================
public class CommunityAnswerConfig : IEntityTypeConfiguration<CommunityAnswer>
{
    public void Configure(EntityTypeBuilder<CommunityAnswer> b)
    {
        b.ToTable("community_answers");
        PgConfig.Base(b);
        b.Property(x => x.QuestionId).HasColumnName("question_id").IsRequired();
        b.Property(x => x.AuthorId).HasColumnName("author_id").IsRequired();
        b.Property(x => x.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        b.Property(x => x.VoteScore).HasColumnName("vote_score").HasDefaultValue(0);
        b.Property(x => x.IsAccepted).HasColumnName("is_accepted").HasDefaultValue(false);

        b.HasIndex(x => x.QuestionId);
        b.HasIndex(x => x.AuthorId);

        b.HasOne(x => x.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ⚠️ community_answers.author_id -> users (cascade) AND
        // community_answers.question_id -> community_questions -> users (cascade)
        // create two different cascade paths that both end up at users. Postgres
        // allows multiple cascade paths, but the actual user-delete behavior
        // should still be tested carefully.
        b.HasOne(x => x.Author)
            .WithMany(u => u.CommunityAnswers)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ============================================================
// 22. community_question_tags
// ============================================================
public class CommunityQuestionTagConfig : IEntityTypeConfiguration<CommunityQuestionTag>
{
    public void Configure(EntityTypeBuilder<CommunityQuestionTag> b)
    {
        b.ToTable("community_question_tags");
        b.HasKey(x => new { x.QuestionId, x.TagId });
        b.Property(x => x.QuestionId).HasColumnName("question_id");
        b.Property(x => x.TagId).HasColumnName("tag_id");
        b.Property(x => x.EffDate)
            .HasColumnName("eff_date")
            .HasDefaultValueSql("date_trunc('day', now())")
            .ValueGeneratedOnAdd();
        b.HasIndex(x => x.TagId);

        b.HasOne(x => x.Question)
            .WithMany(q => q.CommunityQuestionTags)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Tag)
            .WithMany(t => t.CommunityQuestionTags)
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}