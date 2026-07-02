using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewbieCoder.API.Middlewares;
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

    public TestRateLimitService RateLimitService => _rateLimit;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

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
            // Replace AppDbContext with in-memory database.
            var dbContextDescriptors = services.Where(sd =>
                sd.ServiceType == typeof(AppDbContext) ||
                (sd.ServiceType.IsGenericType && sd.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
            ).ToList();
            foreach (var d in dbContextDescriptors) services.Remove(d);
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            // Remove real IAuthRateLimitService.
            var rateLimitDescriptors = services.Where(sd => sd.ServiceType == typeof(IAuthRateLimitService)).ToList();
            foreach (var d in rateLimitDescriptors) services.Remove(d);
            services.AddSingleton<IAuthRateLimitService>(_rateLimit);

            // Override JwtMiddlewareSettings so AuthMiddleware uses the test secret/issuer/audience.
            services.Remove(services.Single(sd => sd.ServiceType == typeof(JwtMiddlewareSettings)));
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
