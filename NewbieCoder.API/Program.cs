using Microsoft.EntityFrameworkCore;
using NewbieCoder.API.Extensions;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Data.SeedData;
using DotNetEnv;

// Load .env BEFORE CreateBuilder so IConfiguration can pick up env vars.
// AppContext.BaseDirectory = NewbieCoder.API/bin/Debug/net8.0/ → .env nằm ngay đó.
var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
DotNetEnv.Env.Load(envPath);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);

// Seed data at startup (idempotent — skips if already seeded).
var seedEnabled = builder.Configuration.GetValue<bool>("SeedData:Enabled");
if (seedEnabled)
{
    builder.Services.AddScoped<AuthDbSeeder>();
}

var app = builder.Build();

// Fix database constraints at startup (idempotent)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await FixDatabaseConstraintsAsync(db);
}

// Run seeder after the DB is ready but before the pipeline starts.
if (seedEnabled)
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();
    await seeder.SeedAsync();
}

app.UseApiPipeline();

app.Run();

// Ensure database constraints are correct
static async Task FixDatabaseConstraintsAsync(AppDbContext db)
{
    try
    {
        // Fix ck_users_status constraint to include 'LOCKED'
        await db.Database.ExecuteSqlRawAsync(@"
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_tables WHERE tablename = 'users') THEN
                    ALTER TABLE users DROP CONSTRAINT IF EXISTS ck_users_status;
                    ALTER TABLE users ADD CONSTRAINT ck_users_status
                        CHECK (status IN ('ACT','INACT','BAN','CLS','LOCKED'));
                END IF;
            END $$;
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not fix database constraints: {ex.Message}");
    }
}

// Expose for integration tests
public partial class Program;
