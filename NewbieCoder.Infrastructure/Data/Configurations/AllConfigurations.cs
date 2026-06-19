using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewbieCoder.Core.Entities;

namespace NewbieCoder.Infrastructure.Persistence.Configurations;

internal static class PgConfig
{
    public static void Base<TEntity>(EntityTypeBuilder<TEntity> b) where TEntity : BaseEntity
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityAlwaysColumn();
        b.Property(x => x.EffDate).HasColumnName("eff_date").HasDefaultValueSql("date_trunc('day', now())");
        b.Property(x => x.DateLastMaint).HasColumnName("date_last_maint").HasDefaultValueSql("now()");
        b.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        b.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        b.HasIndex(x => x.DeletedAt).HasFilter("deleted_at IS NULL");
    }
}

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
        b.Property(x => x.LastKnownIp).HasColumnName("last_known_ip").HasMaxLength(45);
        b.Property(x => x.ReputationScore).HasColumnName("reputation_score");
        b.Property(x => x.FollowerCount).HasColumnName("follower_count").HasDefaultValue(0);
        b.Property(x => x.FollowingCount).HasColumnName("following_count").HasDefaultValue(0);
        b.Property(x => x.PostCount).HasColumnName("post_count").HasDefaultValue(0);
        b.Property(x => x.EmailVerified).HasColumnName("email_verified").HasDefaultValue(false);
        b.Property(x => x.EmailVerifiedAt).HasColumnName("email_verified_at");
        b.Property(x => x.LastLoginAt).HasColumnName("last_login_at");

        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.Username).IsUnique();

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_users_status", "status IN ('ACT','INACT','BAN','CLS')");
            t.HasCheckConstraint("ck_users_reputation", "reputation_score IS NULL OR reputation_score <= 100");
            t.HasCheckConstraint("ck_users_password_len", "length(password) >= 60");
            t.HasCheckConstraint("ck_users_follower_cnt", "follower_count >= 0");
            t.HasCheckConstraint("ck_users_following_cnt", "following_count >= 0");
            t.HasCheckConstraint("ck_users_post_cnt", "post_count >= 0");
        });
    }
}

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
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(10).HasDefaultValue("ACT");
        b.HasIndex(x => x.Code).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_roles_status", "status IN ('ACT','INACT')"));
    }
}

public class PermissionConfig : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("permissions");
        PgConfig.Base(b);
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        b.Property(x => x.Module).HasColumnName("module").HasMaxLength(100).IsRequired();
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(10).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(10).HasDefaultValue("ACT");
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.Module);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_permissions_action", "action IN ('VIEW','INIT','EDIT','DEL','APPR','RJCT','ASIG')");
            t.HasCheckConstraint("ck_permissions_status", "status IN ('ACT','INACT')");
        });
    }
}

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
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(10).HasDefaultValue("ACT");
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.RoleId);
        b.ToTable(t => t.HasCheckConstraint("ck_user_roles_status", "status IN ('ACT','RVKD','EXP')"));
    }
}

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
    }
}
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
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(10).HasDefaultValue("ACT");
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.ParentId);
        b.ToTable(t => t.HasCheckConstraint("ck_post_cat_status", "status IN ('ACT','INACT')"));
    }
}

public class TagConfig : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.ToTable("tags");
        PgConfig.Base(b);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(10).HasDefaultValue("ACT");
        b.Property(x => x.PostCount).HasColumnName("post_count").HasDefaultValue(0);
        b.Property(x => x.QuestionCount).HasColumnName("question_count").HasDefaultValue(0);
        b.Property(x => x.FollowerCount).HasColumnName("follower_count").HasDefaultValue(0);
        b.HasIndex(x => x.Slug).IsUnique();
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_tags_status", "status IN ('ACT','INACT')");
            t.HasCheckConstraint("ck_tags_post_cnt", "post_count >= 0");
            t.HasCheckConstraint("ck_tags_q_cnt", "question_count >= 0");
            t.HasCheckConstraint("ck_tags_follow_cnt", "follower_count >= 0");
        });
    }
}

public class PostConfig : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> b)
    {
        b.ToTable("posts");
        PgConfig.Base(b);
        b.Property(x => x.AuthorId).HasColumnName("author_id");
        b.Property(x => x.CategoryId).HasColumnName("category_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(500).IsRequired();
        b.Property(x => x.Summary).HasColumnName("summary").HasColumnType("text");
        b.Property(x => x.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        b.Property(x => x.ThumbnailUrl).HasColumnName("thumbnail_url").HasColumnType("text");
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("DRAFT");
        b.Property(x => x.Visibility).HasColumnName("visibility").HasMaxLength(15).HasDefaultValue("PUBLIC");
        b.Property(x => x.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
        b.Property(x => x.CommentCount).HasColumnName("comment_count").HasDefaultValue(0);
        b.Property(x => x.VoteScore).HasColumnName("vote_score").HasDefaultValue(0);
        b.Property(x => x.BookmarkCount).HasColumnName("bookmark_count").HasDefaultValue(0);
        b.Property(x => x.PublishedAt).HasColumnName("published_at");
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.AuthorId);
        b.HasIndex(x => x.CategoryId);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_posts_status", "status IN ('DRAFT','PENDING','PUB','RJCT','HIDDEN','DEL')");
            t.HasCheckConstraint("ck_posts_visibility", "visibility IN ('PUBLIC','PRIVATE','LINK')");
            t.HasCheckConstraint("ck_posts_view_cnt", "view_count >= 0");
            t.HasCheckConstraint("ck_posts_comment_cnt", "comment_count >= 0");
            t.HasCheckConstraint("ck_posts_bookmark_cnt", "bookmark_count >= 0");
        });
    }
}

public class PostTagConfig : IEntityTypeConfiguration<PostTag>
{
    public void Configure(EntityTypeBuilder<PostTag> b)
    {
        b.ToTable("post_tags");
        b.HasKey(x => new { x.PostId, x.TagId });
        b.Property(x => x.PostId).HasColumnName("post_id");
        b.Property(x => x.TagId).HasColumnName("tag_id");
        b.Property(x => x.EffDate).HasColumnName("eff_date").HasDefaultValueSql("date_trunc('day', now())");
        b.HasIndex(x => x.TagId);
    }
}

public class SeriesConfig : IEntityTypeConfiguration<Series>
{
    public void Configure(EntityTypeBuilder<Series> b)
    {
        b.ToTable("series");
        PgConfig.Base(b);
        b.Property(x => x.AuthorId).HasColumnName("author_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(500).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        b.Property(x => x.ThumbnailUrl).HasColumnName("thumbnail_url").HasColumnType("text");
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("DRAFT");
        b.Property(x => x.Visibility).HasColumnName("visibility").HasMaxLength(15).HasDefaultValue("PUBLIC");
        b.Property(x => x.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
        b.Property(x => x.BookmarkCount).HasColumnName("bookmark_count").HasDefaultValue(0);
        b.Property(x => x.PublishedAt).HasColumnName("published_at");
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.AuthorId);
    }
}

public class SeriesPostConfig : IEntityTypeConfiguration<SeriesPost>
{
    public void Configure(EntityTypeBuilder<SeriesPost> b)
    {
        b.ToTable("series_posts");
        b.HasKey(x => new { x.SeriesId, x.PostId });
        b.Property(x => x.SeriesId).HasColumnName("series_id");
        b.Property(x => x.PostId).HasColumnName("post_id");
        b.Property(x => x.Position).HasColumnName("position").HasDefaultValue(1);
        b.Property(x => x.EffDate).HasColumnName("eff_date").HasDefaultValueSql("date_trunc('day', now())");
        b.HasIndex(x => new { x.SeriesId, x.Position }).IsUnique();
    }
}
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
        b.Property(x => x.Level).HasColumnName("level").HasMaxLength(10).IsRequired();
        b.Property(x => x.Topic).HasColumnName("topic").HasMaxLength(100);
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("DRAFT");
        b.Property(x => x.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
        b.Property(x => x.VoteScore).HasColumnName("vote_score").HasDefaultValue(0);
        b.Property(x => x.BookmarkCount).HasColumnName("bookmark_count").HasDefaultValue(0);
        b.Property(x => x.PublishedAt).HasColumnName("published_at");
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.AuthorId);
    }
}

public class InterviewAnswerConfig : IEntityTypeConfiguration<InterviewAnswer>
{
    public void Configure(EntityTypeBuilder<InterviewAnswer> b)
    {
        b.ToTable("interview_answers");
        PgConfig.Base(b);
        b.Property(x => x.QuestionId).HasColumnName("question_id");
        b.Property(x => x.AnswerContent).HasColumnName("answer_content").HasColumnType("text").IsRequired();
        b.Property(x => x.Explanation).HasColumnName("explanation").HasColumnType("text");
        b.Property(x => x.Example).HasColumnName("example").HasColumnType("text");
        b.Property(x => x.IsOfficial).HasColumnName("is_official").HasDefaultValue(true);
        b.HasIndex(x => x.QuestionId);
    }
}

public class InterviewQuestionTagConfig : IEntityTypeConfiguration<InterviewQuestionTag>
{
    public void Configure(EntityTypeBuilder<InterviewQuestionTag> b)
    {
        b.ToTable("interview_question_tags");
        b.HasKey(x => new { x.QuestionId, x.TagId });
        b.Property(x => x.QuestionId).HasColumnName("question_id");
        b.Property(x => x.TagId).HasColumnName("tag_id");
        b.Property(x => x.EffDate).HasColumnName("eff_date").HasDefaultValueSql("date_trunc('day', now())");
        b.HasIndex(x => x.TagId);
    }
}

public class CommunityQuestionConfig : IEntityTypeConfiguration<CommunityQuestion>
{
    public void Configure(EntityTypeBuilder<CommunityQuestion> b)
    {
        b.ToTable("community_questions");
        PgConfig.Base(b);
        b.Property(x => x.AuthorId).HasColumnName("author_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(500).IsRequired();
        b.Property(x => x.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(10).HasDefaultValue("OPEN");
        b.Property(x => x.AcceptedAnswerId).HasColumnName("accepted_answer_id");
        b.Property(x => x.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
        b.Property(x => x.AnswerCount).HasColumnName("answer_count").HasDefaultValue(0);
        b.Property(x => x.VoteScore).HasColumnName("vote_score").HasDefaultValue(0);
        b.Property(x => x.BookmarkCount).HasColumnName("bookmark_count").HasDefaultValue(0);
        b.Property(x => x.ClosedAt).HasColumnName("closed_at");
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => x.AuthorId);
        b.HasIndex(x => x.AcceptedAnswerId);
    }
}

public class CommunityAnswerConfig : IEntityTypeConfiguration<CommunityAnswer>
{
    public void Configure(EntityTypeBuilder<CommunityAnswer> b)
    {
        b.ToTable("community_answers");
        PgConfig.Base(b);
        b.Property(x => x.QuestionId).HasColumnName("question_id");
        b.Property(x => x.AuthorId).HasColumnName("author_id");
        b.Property(x => x.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        b.Property(x => x.VoteScore).HasColumnName("vote_score").HasDefaultValue(0);
        b.Property(x => x.IsAccepted).HasColumnName("is_accepted").HasDefaultValue(false);
        b.HasIndex(x => x.QuestionId);
        b.HasIndex(x => x.AuthorId);
    }
}

public class CommunityQuestionTagConfig : IEntityTypeConfiguration<CommunityQuestionTag>
{
    public void Configure(EntityTypeBuilder<CommunityQuestionTag> b)
    {
        b.ToTable("community_question_tags");
        b.HasKey(x => new { x.QuestionId, x.TagId });
        b.Property(x => x.QuestionId).HasColumnName("question_id");
        b.Property(x => x.TagId).HasColumnName("tag_id");
        b.Property(x => x.EffDate).HasColumnName("eff_date").HasDefaultValueSql("date_trunc('day', now())");
        b.HasIndex(x => x.TagId);
    }
}
