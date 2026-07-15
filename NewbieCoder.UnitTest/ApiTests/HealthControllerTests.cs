using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewbieCoder.API.Middlewares;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Services;

namespace NewbieCoder.UnitTest.ApiTests;

/// <summary>
/// Shared base factory with test configuration (JWT secrets, in-memory DB, no seeder).
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly TestRateLimitService _rateLimit = new();

    /// <summary>
    /// Static seed flag + lock ensures seeding runs exactly once across all factory subclasses and
    /// all CreateHost calls, even when called concurrently by xUnit parallel test runners.
    /// Static DB name ensures all subclasses share the same in-memory database,
    /// so the seed from the first CreateHost call is visible to all factories and all tests.
    /// </summary>
    private static bool _seeded;
    private static readonly object _seedLock = new();
    private const string SharedInMemoryDbName = "TestBlogApiDb";

    static TestWebApplicationFactory()
    {
        var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
        Directory.CreateDirectory(uploadsDir);

        Environment.SetEnvironmentVariable("JwtSettings__Secret", "TestSecretKeyThatIsAtLeast32CharactersLongForJwt!");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "NewbieCoderAPI");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "NewbieCoderClient");
    }

    public TestRateLimitService RateLimitService => _rateLimit;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Seed exactly once per test process (static lock prevents concurrent re-entry from
        // parallel xUnit test runners). Without this, AuthMiddleware.IsAccountLockedAsync queries
        // an empty in-memory DB and treats the token bearer as locked (returning 403 Forbidden).
        lock (_seedLock)
        {
            if (_seeded) return host;
            _seeded = true;

            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!db.Users.Any())
            {
                db.Users.Add(new User
                {
                    Id = 1,
                    Email = "test@example.com",
                    Username = "testuser",
                    FullName = "Test User",
                    Password = "dummy-hash",
                    Location = "Test City",
                    Status = UserStatus.Active,
                    EmailVerified = true,
                    EffDate = DateTimeOffset.UtcNow,
                    DateLastMaint = DateTimeOffset.UtcNow
                });

                db.Roles.Add(new Role
                {
                    Id = 1,
                    Code = "USER",
                    Name = "User",
                    IsSystem = true,
                    Status = RoleStatus.Active,
                    EffDate = DateTimeOffset.UtcNow,
                    DateLastMaint = DateTimeOffset.UtcNow
                });

                db.UserRoles.Add(new UserRole
                {
                    UserId = 1,
                    RoleId = 1,
                    Status = UserRoleStatus.Active,
                    AssignedAt = DateTimeOffset.UtcNow,
                    EffDate = DateTimeOffset.UtcNow,
                    DateLastMaint = DateTimeOffset.UtcNow
                });

                db.SaveChanges();
            }
        }

        return host;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.UseSetting("JwtSettings__Secret", "TestSecretKeyThatIsAtLeast32CharactersLongForJwt!");
        builder.UseSetting("JwtSettings__Issuer", "NewbieCoderAPI");
        builder.UseSetting("JwtSettings__Audience", "NewbieCoderClient");

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedData:Enabled"] = "false",
                ["ConnectionStrings:DefaultConnection"] = "InMemoryConnection"
            }!);
        });

        builder.ConfigureServices(services =>
        {
            // Replace AppDbContext with in-memory database using the instance name so all scoped
            // resolutions share the same database (required for CreateServer seed to be visible).
            var dbContextDescriptors = services.Where(sd =>
                sd.ServiceType == typeof(AppDbContext) ||
                (sd.ServiceType.IsGenericType && sd.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
            ).ToList();
            foreach (var d in dbContextDescriptors) services.Remove(d);
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(SharedInMemoryDbName));

            // Remove real IAuthRateLimitService.
            var rateLimitDescriptors = services.Where(sd => sd.ServiceType == typeof(IAuthRateLimitService)).ToList();
            foreach (var d in rateLimitDescriptors) services.Remove(d);
            services.AddSingleton<IAuthRateLimitService>(_rateLimit);

            // Override JwtMiddlewareSettings.
            var jwtSettingsDescriptor = services.SingleOrDefault(sd => sd.ServiceType == typeof(JwtMiddlewareSettings));
            if (jwtSettingsDescriptor != null) services.Remove(jwtSettingsDescriptor);
            services.AddSingleton(new JwtMiddlewareSettings
            {
                Secret = "TestSecretKeyThatIsAtLeast32CharactersLongForJwt!",
                Issuer = "NewbieCoderAPI",
                Audience = "NewbieCoderClient"
            });
        });
    }

    internal void SetRateLimitBlocked(bool blocked) => _rateLimit.SetBlocked(blocked);
}

public class HealthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
