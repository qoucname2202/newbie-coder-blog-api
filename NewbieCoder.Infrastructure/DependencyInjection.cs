using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.Options;
using NewbieCoder.Infrastructure.CQRS.Posts;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Repositories;
using NewbieCoder.Infrastructure.Services;
using NewbieCoder.Infrastructure.UnitOfWork;

namespace NewbieCoder.Infrastructure;

public static class DependencyInjection
{
    private static bool _envLoaded;

    static DependencyInjection()
    {
        LoadEnvironmentVariables();
    }

    private static void LoadEnvironmentVariables()
    {
        if (_envLoaded) return;

        var envPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, ".env"),
            Path.Combine(AppContext.BaseDirectory, "..", ".env"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
        };

        foreach (var envPath in envPaths)
        {
            var normalizedPath = Path.GetFullPath(envPath);
            if (File.Exists(normalizedPath))
            {
                DotNetEnv.Env.Load(normalizedPath);
                break;
            }
        }

        _envLoaded = true;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b =>
                {
                    b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    b.CommandTimeout(30);
                    b.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                }));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // Auth services
        services.AddSingleton<IAuthRateLimitService, AuthRateLimitService>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService>(sp =>
            new AuthService(
                sp.GetRequiredService<AppDbContext>(),
                sp.GetRequiredService<JwtSettings>(),
                sp.GetRequiredService<IPasswordHasherService>(),
                sp.GetRequiredService<IAuthRateLimitService>(),
                sp.GetRequiredService<IAuditLogService>()));

        services.AddSingleton<IEmailService, StubEmailService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        // User management services
        services.AddScoped<IUserService, UserService>();
        // File upload service
  
        services.AddScoped<IFileUploadService, FileUploadService>();

        // Role management services
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRoleService, RoleService>();

        // Post management services
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IAdminPostService, AdminPostService>();
        services.AddScoped<ChangePostStatusCommandHandler>();

        // Interview question management services
        services.AddScoped<IInterviewQuestionRepository, InterviewQuestionRepository>();
        services.AddScoped<IInterviewQuestionService, InterviewQuestionService>();

        // Category management services
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryService, CategoryService>();

        // Tag management services
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ITagService, TagService>();

        // Image storage services - read Cloudinary credentials from environment variables
        // Supports both CLOUDINARY_* format and Cloudinary__* format from .env
        services.Configure<CloudinaryOptions>(options =>
        {
            options.CloudName = Environment.GetEnvironmentVariable("CLOUDINARY_NAME")
                             ?? Environment.GetEnvironmentVariable("Cloudinary__CloudName")
                             ?? string.Empty;
            options.ApiKey = Environment.GetEnvironmentVariable("CLOUDINARY_KEY")
                           ?? Environment.GetEnvironmentVariable("Cloudinary__ApiKey")
                           ?? string.Empty;
            options.ApiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_SECRET")
                              ?? Environment.GetEnvironmentVariable("Cloudinary__ApiSecret")
                              ?? string.Empty;
        });

        services.Configure<ImageUploadOptions>(configuration.GetSection(ImageUploadOptions.SectionName)!);

        services.AddScoped<IImageStorageService, ImageStorageService>();
        // Level management services
        services.AddScoped<ILevelRepository, LevelRepository>();
        services.AddScoped<ILevelService, LevelService>();

        // Community question management services
        services.AddScoped<ICommunityQuestionRepository, CommunityQuestionRepository>();
        services.AddScoped<ICommunityQuestionService, CommunityQuestionService>();

        // Community answer management services
        services.AddScoped<ICommunityAnswerRepository, CommunityAnswerRepository>();
        services.AddScoped<ICommunityAnswerService, CommunityAnswerService>();

        return services;
    }
}
