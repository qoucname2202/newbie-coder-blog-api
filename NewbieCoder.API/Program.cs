using Microsoft.EntityFrameworkCore;
using NewbieCoder.API.Extensions;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Data.SeedData;
using DotNetEnv;

// Load .env BEFORE CreateBuilder so IConfiguration can pick up env vars.
// Try multiple locations: output dir, project dir, and current directory.
var possibleEnvPaths = new[]
{
    Path.Combine(AppContext.BaseDirectory, ".env"),
    Path.Combine(AppContext.BaseDirectory, "..", ".env"),
    Path.Combine(AppContext.BaseDirectory, "..", "..", ".env"),
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"),
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"),
};

string? loadedEnvPath = null;
foreach (var envPath in possibleEnvPaths)
{
    var normalizedPath = Path.GetFullPath(envPath);
    if (File.Exists(normalizedPath))
    {
        DotNetEnv.Env.Load(normalizedPath);
        loadedEnvPath = normalizedPath;
        Console.WriteLine($"Loaded .env from: {normalizedPath}");
        break;
    }
}

if (loadedEnvPath == null)
{
    Console.WriteLine("Warning: .env file not found in any of the expected locations.");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);

// Seed data at startup (idempotent — skips if already seeded).
var seedEnabled = builder.Configuration.GetValue<bool>("SeedData:Enabled");
if (seedEnabled)
{
    builder.Services.AddScoped<AuthDbSeeder>();
}

var app = builder.Build();

// Run seeder after the DB is ready but before the pipeline starts.
if (seedEnabled)
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();
    await seeder.SeedAsync();
}

// Ensure uploads directory exists (used by UseStaticFiles).
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "uploads"));

app.UseApiPipeline();

app.Run();

// Expose for integration tests
public partial class Program;
