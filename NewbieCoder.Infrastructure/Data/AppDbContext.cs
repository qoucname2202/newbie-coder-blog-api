using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.Entities;

namespace NewbieCoder.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
   
    public DbSet<PostCategory> PostCategories => Set<PostCategory>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostTag> PostTags => Set<PostTag>();
    public DbSet<Series> Series => Set<Series>();
    public DbSet<SeriesPost> SeriesPosts => Set<SeriesPost>();
   
    public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();
    public DbSet<InterviewAnswer> InterviewAnswers => Set<InterviewAnswer>();
    public DbSet<InterviewQuestionTag> InterviewQuestionTags => Set<InterviewQuestionTag>();
    
    public DbSet<CommunityQuestion> CommunityQuestions => Set<CommunityQuestion>();
    public DbSet<CommunityAnswer> CommunityAnswers => Set<CommunityAnswer>();
    public DbSet<CommunityQuestionTag> CommunityQuestionTags => Set<CommunityQuestionTag>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
