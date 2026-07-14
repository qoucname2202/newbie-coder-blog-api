using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Repositories;
using NewbieCoder.Infrastructure.Services;
using NewbieCoder.Infrastructure.UnitOfWork;

namespace NewbieCoder.Infrastructure;

public static class DependencyInjection
{
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
                }));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // Auth services
        services.AddSingleton<IAuthRateLimitService, AuthRateLimitService>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService>(sp =>
            new AuthService(
                sp.GetRequiredService<AppDbContext>(),
                sp.GetRequiredService<JwtSettings>(),
                sp.GetRequiredService<IPasswordHasherService>(),
                sp.GetRequiredService<IAuthRateLimitService>(),
                sp.GetRequiredService<IAuditLogService>()));

        // User management services
        services.AddScoped<IUserService, UserService>();
        // File upload service
  
        services.AddScoped<IFileUploadService, FileUploadService>();

        return services;
    }
}
