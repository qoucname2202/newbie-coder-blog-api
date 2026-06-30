using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Data.SeedData;

/// <summary>
/// Seeds the initial set of system roles and default admin/test accounts.
/// Runs automatically at application startup when <c>SeedData:Enabled</c> is <c>true</c>.
/// Fully idempotent — safe to call multiple times.
/// </summary>
public sealed class AuthDbSeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuthDbSeeder> _logger;

    public AuthDbSeeder(AppDbContext db, ILogger<AuthDbSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Seeds roles and default accounts if not already present.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AuthDbSeeder: starting seed...");

        await SeedRolesAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);

        _logger.LogInformation("AuthDbSeeder: seed completed.");
    }

    // ──────────────────────────────────────────────────────────────
    // Roles
    // ──────────────────────────────────────────────────────────────

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        var existingCodes = await _db.Roles
            .Where(r => RoleConstants.AllCodes.Contains(r.Code))
            .Select(r => r.Code)
            .ToListAsync(ct);

        var toInsert = RoleConstants.AllCodes
            .Where(code => !existingCodes.Contains(code))
            .Select(code => new Role
            {
                Code = code,
                Name = AuthSeedData.RoleDisplayNames[code],
                Description = AuthSeedData.RoleDescriptions[code],
                IsSystem = true,
                Status = RoleStatus.Active
            })
            .ToList();

        if (toInsert.Count > 0)
        {
            _db.Roles.AddRange(toInsert);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "AuthDbSeeder: inserted {Count} roles: {Codes}",
                toInsert.Count,
                string.Join(", ", toInsert.Select(r => r.Code)));
        }
        else
        {
            _logger.LogInformation("AuthDbSeeder: all roles already exist.");
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Users
    // ──────────────────────────────────────────────────────────────

    private async Task SeedUsersAsync(CancellationToken ct)
    {
        var roleMap = await _db.Roles
            .Where(r => RoleConstants.AllCodes.Contains(r.Code))
            .ToDictionaryAsync(r => r.Code, r => r.Id, ct);

        foreach (var entry in UserSeedData.Accounts)
        {
            var exists = await _db.Users.AnyAsync(u => u.Email == entry.Email, ct);
            if (exists)
            {
                _logger.LogInformation("AuthDbSeeder: user {Email} already exists, skipping.", entry.Email);
                continue;
            }

            var user = new User
            {
                Email         = entry.Email,
                Username      = entry.Username,
                FullName      = entry.FullName,
                Password      = entry.Password,
                Bio           = entry.Bio,
                Location      = entry.Location,
                Status        = entry.Status,
                EmailVerified = entry.EmailVerified,
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            foreach (var roleCode in entry.Roles)
            {
                if (!roleMap.TryGetValue(roleCode, out var roleId))
                {
                    _logger.LogWarning(
                        "AuthDbSeeder: role {RoleCode} not found for user {Email}, skipping assignment.",
                        roleCode, entry.Email);
                    continue;
                }

                _db.UserRoles.Add(new UserRole
                {
                    UserId     = user.Id,
                    RoleId     = roleId,
                    AssignedBy = null,
                    Status     = UserRoleStatus.Active
                });
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "AuthDbSeeder: created user {Email} with roles [{Roles}].",
                entry.Email,
                string.Join(", ", entry.Roles));
        }
    }
}
