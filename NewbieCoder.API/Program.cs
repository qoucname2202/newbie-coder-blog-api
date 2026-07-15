using Microsoft.EntityFrameworkCore;
using NewbieCoder.API.Extensions;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Data.SeedData;
using DotNetEnv;

// Load .env BEFORE CreateBuilder so IConfiguration can pick up env vars.
var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
DotNetEnv.Env.Load(envPath);
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

// Auto-apply any pending schema changes on startup (idempotent — skips if column already exists).
// Retry up to 3 times with exponential backoff in case Neon is temporarily unreachable.
async Task<bool> TrySchemaMigrate(AppDbContext db, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                @"ALTER TABLE users ADD COLUMN IF NOT EXISTS password_changed_at TIMESTAMP WITH TIME ZONE NULL;");
            return true;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            Console.WriteLine($"[WARN] Schema migration attempt {attempt} failed: {ex.Message}. Retrying in {delay.TotalSeconds}s...");
            await Task.Delay(delay);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Schema migration failed after {maxRetries} attempts (may already exist): {ex.Message}");
            return false;
        }
    }
    return false;
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await TrySchemaMigrate(db);
}

// Run seeder after the DB is ready but before the pipeline starts.
if (seedEnabled)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[WARN] AuthDbSeeder failed: {ex.Message}");
    }
}

// Ensure uploads directory exists (used by UseStaticFiles).
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "uploads"));

app.UseApiPipeline();

app.Run();

// Expose for integration tests
public partial class Program;
