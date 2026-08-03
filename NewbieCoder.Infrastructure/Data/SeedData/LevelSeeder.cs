using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewbieCoder.Core.Entities;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Data.SeedData;

/// <summary>
/// Seeds the default interview question levels.
/// Runs automatically at application startup when <c>SeedData:Enabled</c> is <c>true</c>.
/// Executes exactly once — safe to call multiple times.
/// </summary>
public sealed class LevelSeeder
{
    private const string SeedKey = "level_seed_v1";

    private static readonly (string Code, string Name, string Description, int DisplayOrder)[] DefaultLevels =
    [
        ("ENTRY", "Entry", "Entry-level interview questions for beginners.", 1),
        ("JUNIOR", "Junior", "Junior-level interview questions for candidates with 0-2 years of experience.", 2),
        ("MIDDLE", "Middle", "Middle-level interview questions for candidates with 2-5 years of experience.", 3),
        ("SENIOR", "Senior", "Senior-level interview questions for candidates with 5-10 years of experience.", 4),
        ("EXPERT", "Expert", "Expert-level interview questions for candidates with 10+ years of experience.", 5),
    ];

    private readonly AppDbContext _db;
    private readonly ILogger<LevelSeeder> _logger;

    public LevelSeeder(AppDbContext db, ILogger<LevelSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the default levels if not already seeded.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var alreadySeeded = await _db.SeedFlags.AnyAsync(f => f.Key == SeedKey, cancellationToken);
        if (alreadySeeded)
        {
            _logger.LogInformation("LevelSeeder: already seeded, skipping.");
            return;
        }

        _logger.LogInformation("LevelSeeder: starting seed...");

        var existingCodes = await _db.Levels
            .Select(l => l.Code.ToUpper())
            .ToListAsync(cancellationToken);

        var toInsert = DefaultLevels
            .Where(l => !existingCodes.Contains(l.Code))
            .Select(l => new Level
            {
                Code = l.Code,
                Name = l.Name,
                Description = l.Description,
                DisplayOrder = l.DisplayOrder,
                IsActive = true,
                EffDate = DateTimeOffset.UtcNow,
                DateLastMaint = DateTimeOffset.UtcNow
            })
            .ToList();

        if (toInsert.Count > 0)
        {
            _db.Levels.AddRange(toInsert);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "LevelSeeder: inserted {Count} levels: {Codes}",
                toInsert.Count,
                string.Join(", ", toInsert.Select(l => l.Code)));
        }
        else
        {
            _logger.LogInformation("LevelSeeder: all levels already exist.");
        }

        _db.SeedFlags.Add(new SeedFlag { Key = SeedKey, SeededAt = DateTimeOffset.UtcNow });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("LevelSeeder: seed completed.");
    }
}
