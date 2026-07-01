using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Data;

/// <summary>
/// Factory used by EF Core CLI tools (migrations, database update) to create
/// an AppDbContext instance without needing the full application runtime.
/// Reads the connection string from the DATABASE_URL environment variable.
/// Falls back to the .env file in the API project directory.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        // 1. Explicit environment variable takes priority.
        var fromEnv = Environment.GetEnvironmentVariable("DATABASE_URL",
            EnvironmentVariableTarget.Process);
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        // 2. Look for a .env file next to the API project.
        var envPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "NewbieCoder.API", ".env");

        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2 &&
                    parts[0].Trim().Equals("DATABASE_URL", StringComparison.OrdinalIgnoreCase))
                {
                    return parts[1].Trim();
                }
            }
        }

        // 3. Fallback: read from appsettings.Development.json in the API project.
        var settingsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "NewbieCoder.API", "appsettings.Development.json");

        if (File.Exists(settingsPath))
        {
            var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(settingsPath));
            var connStr = json.RootElement
                .GetProperty("ConnectionStrings")
                .GetProperty("DefaultConnection")
                .GetString();
            if (!string.IsNullOrWhiteSpace(connStr))
                return connStr;
        }

        throw new InvalidOperationException(
            "DATABASE_URL environment variable is not set and .env file not found. "
            + "Run 'dotnet ef migrations' from a shell where DATABASE_URL is defined, e.g.: "
            + "set DATABASE_URL=Host=localhost;Database=... && dotnet ef migrations add ...");
    }
}
