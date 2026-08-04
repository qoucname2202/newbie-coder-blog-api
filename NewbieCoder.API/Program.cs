using Microsoft.EntityFrameworkCore;
using NewbieCoder.API.Extensions;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Data.SeedData;
using DotNetEnv;

// Try multiple locations: output dir, project dir, and current directory.
// ── Load .env BEFORE building the host so all services & middleware
//    see the environment variables from the start. ──────────────────────────
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
foreach (var candidatePath in possibleEnvPaths)
{
    var normalizedPath = Path.GetFullPath(candidatePath);
        if (File.Exists(normalizedPath))
        {
            DotNetEnv.Env.Load(normalizedPath);

        // Push all KEY=VALUE lines into the process environment so both
        // IConfiguration (Reload()ed later) and Environment.GetEnvironmentVariable
        // can read them regardless of when they are called.
        foreach (var line in File.ReadAllLines(normalizedPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;
            var eq = trimmed.IndexOf('=');
            if (eq < 0) continue;
            var key = trimmed[..eq].Trim();
            var val = trimmed[(eq + 1)..].Trim();
            Environment.SetEnvironmentVariable(key, val);
        }

        loadedEnvPath = normalizedPath;
        Console.WriteLine($"Loaded .env from: {normalizedPath}");
        break;
    }
}

if (loadedEnvPath == null)
{
    Console.WriteLine("Warning: .env file not found in any of the expected locations.");
}

// ── Build the host AFTER .env is loaded ──────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);

// Seed data at startup (idempotent — skips if already seeded).
var seedEnabled = builder.Configuration.GetValue<bool>("SeedData:Enabled");
if (seedEnabled)
{
    builder.Services.AddScoped<AuthDbSeeder>();
    builder.Services.AddScoped<LevelSeeder>();
}

var app = builder.Build();

// Test database connectivity before running any SQL commands.
// Retries up to 3 times with exponential backoff to survive brief Neon outages.
async Task<bool> TryTestConnection(AppDbContext db, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1;");
            return true;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            Console.WriteLine($"[WARN] Database connectivity test attempt {attempt} failed: {ex.Message}. Retrying in {delay.TotalSeconds}s...");
            await Task.Delay(delay);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Database connectivity test failed after {maxRetries} attempts: {ex.Message}");
            return false;
        }
    }
    return false;
}

// Auto-apply any pending schema changes on startup (idempotent — skips if column already exists).
// Retries up to 3 times with exponential backoff in case Neon is temporarily unreachable.
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

// Skip all relational database operations in Testing environment — integration tests use
// EF Core InMemory which does not support ExecuteSqlRawAsync / MigrateAsync.
var isTesting = app.Environment.IsEnvironment("Testing");
if (isTesting)
{
    Console.WriteLine("[INFO] Testing environment detected — skipping DB connectivity test, schema migration, and seeder.");
}
else
{
    var dbConnected = false;
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbConnected = await TryTestConnection(db);
    }

    // Only run constraint fixes and schema migrations if the database is reachable.
    if (dbConnected)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TrySchemaMigrate(db);
        }
    }
    else
    {
        Console.WriteLine("[WARN] Skipping DB constraint fixes and schema migrations — database is unreachable.");
    }
}

// Run seeder after the DB is ready but before the pipeline starts.
if (seedEnabled && !isTesting)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();
        await seeder.SeedAsync();

        var levelSeeder = scope.ServiceProvider.GetRequiredService<LevelSeeder>();
        await levelSeeder.SeedAsync();
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
public partial class Program
{
}
