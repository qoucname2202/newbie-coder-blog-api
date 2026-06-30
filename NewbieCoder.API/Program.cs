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

// Run seeder after the DB is ready but before the pipeline starts.
if (seedEnabled)
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();
    await seeder.SeedAsync();
}

app.UseApiPipeline();

app.Run();

// Expose for integration tests
public partial class Program;
