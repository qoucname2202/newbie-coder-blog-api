using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Services;

namespace NewbieCoder.UnitTest.CoreTests;

public class UserManagementServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly TestAuditLogService _auditLog;
    private readonly UserManagementService _sut;

    public UserManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _auditLog = new TestAuditLogService();
        _sut = new UserManagementService(_db, _auditLog, NullLogger<UserManagementService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    #region Helpers

    private async Task<User> SeedActiveUserAsync(long id = 1, string email = "user@test.com", string username = "testuser")
    {
        var user = new User
        {
            Id = id,
            Email = email,
            Username = username,
            Password = "HashedPassword",
            FullName = "Test User",
            Location = "Test",
            Status = UserStatus.Active,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task<User> SeedAdminUserAsync(long id = 99, string email = "admin@test.com", string username = "admin")
    {
        // Seed admin role only once per DB instance to avoid track conflicts
        if (!await _db.Roles.AnyAsync(r => r.Id == 1))
        {
            _db.Roles.Add(new Role
            {
                Id = 1,
                Code = RoleConstants.Admin,
                Name = "Admin",
                IsSystem = true,
                Status = RoleStatus.Active,
                EffDate = DateTimeOffset.UtcNow,
                DateLastMaint = DateTimeOffset.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        var admin = new User
        {
            Id = id,
            Email = email,
            Username = username,
            Password = "HashedPassword",
            FullName = "Admin User",
            Location = "Test",
            Status = UserStatus.Active,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.Users.Add(admin);
        await _db.SaveChangesAsync();

        var userRole = new UserRole
        {
            UserId = id,
            RoleId = 1,
            Status = UserRoleStatus.Active,
            AssignedAt = DateTimeOffset.UtcNow,
            EffDate = DateTimeOffset.UtcNow
        };
        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync();

        return admin;
    }

    private async Task SeedActiveSessionForUserAsync(long userId)
    {
        var session = new UserSession
        {
            Id = userId * 100,
            UserId = userId,
            SessionTokenHash = "hash",
            Status = SessionStatus.Active,
            LoginAt = DateTimeOffset.UtcNow,
            ExpiredAt = DateTimeOffset.UtcNow.AddDays(7),
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.UserSessions.Add(session);

        var token = new RefreshToken
        {
            Id = userId * 100,
            UserId = userId,
            SessionId = session.Id,
            TokenHash = "hash",
            Status = TokenStatus.Active,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiredAt = DateTimeOffset.UtcNow.AddDays(7),
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
    }

    #endregion

    #region LockUserAsync — Success Scenarios

    [Fact]
    public async Task LockUserAsync_ActiveUser_SucceedsAndRevokesSessions()
    {
        // Arrange
        var admin = await SeedAdminUserAsync(99);
        var target = await SeedActiveUserAsync(1);
        await SeedActiveSessionForUserAsync(1);

        var request = new LockUserRequest
        {
            Reason = "Violated terms of service"
        };

        // Act
        var result = await _sut.LockUserAsync(
            targetUserId: 1,
            request: request,
            adminUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "Test Agent",
            traceId: "trace-123",
            CancellationToken.None);

        // Assert — DTO matches spec A04: id / status / locked_at / locked_reason / locked_by
        Assert.Equal(1, result.Id);
        Assert.Equal("Locked", result.Status);
        Assert.Equal("Violated terms of service", result.LockedReason);
        Assert.NotNull(result.LockedAt);
        Assert.NotNull(result.LockedBy);
        Assert.Equal(99, result.LockedBy!.Id);
        Assert.Equal("admin", result.LockedBy.Username);

        // Verify DB state
        var dbUser = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == 1);
        Assert.Equal(UserStatus.Locked, dbUser.Status);
        Assert.Equal("Violated terms of service", dbUser.LockedReason);
        // LockedUntil was removed in A04 refactor
    }

    [Fact]
    public async Task LockUserAsync_TrimsReasonWhitespace()
    {
        // Arrange
        var admin = await SeedAdminUserAsync(99);
        var target = await SeedActiveUserAsync(1);

        var request = new LockUserRequest
        {
            Reason = "   Spammy behavior   "
        };

        // Act
        var result = await _sut.LockUserAsync(1, request, 99, null, null, "trace-x", CancellationToken.None);

        // Assert
        Assert.Equal("Spammy behavior", result.LockedReason); // service trims whitespace before storing
        Assert.Equal("Locked", result.Status);
    }

    #endregion

    #region LockUserAsync — Validation / Business Rule Failures

    [Fact]
    public async Task LockUserAsync_UserNotFound_ThrowsBusinessException404()
    {
        // Arrange
        await SeedAdminUserAsync(99);

        var request = new LockUserRequest { Reason = "Test reason" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.LockUserAsync(9999, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.NotFound, ex.ResponseCode);
    }

    [Fact]
    public async Task LockUserAsync_AdminLocksSelf_ThrowsBusinessException403()
    {
        // Arrange
        var admin = await SeedAdminUserAsync(99);

        var request = new LockUserRequest { Reason = "Self lock attempt" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.LockUserAsync(99, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Forbidden, ex.StatusCode);
        Assert.Equal(ResponseCodes.CannotLockSelf, ex.ResponseCode);
    }

    [Fact]
    public async Task LockUserAsync_LockAdminAccount_ThrowsBusinessException403()
    {
        // Arrange
        var admin = await SeedAdminUserAsync(99);
        var targetAdmin = await SeedAdminUserAsync(50, "admin2@test.com", "admin2");

        var request = new LockUserRequest { Reason = "Admin violation" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.LockUserAsync(50, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Forbidden, ex.StatusCode);
        Assert.Equal(ResponseCodes.CannotLockSuperAdmin, ex.ResponseCode);
    }

    [Fact]
    public async Task LockUserAsync_DeletedUser_ThrowsBusinessException409()
    {
        // Arrange
        var admin = await SeedAdminUserAsync(99);
        var target = await SeedActiveUserAsync(1);
        target.DeletedAt = DateTimeOffset.UtcNow;
        target.DateLastMaint = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var request = new LockUserRequest { Reason = "Deleted user" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.LockUserAsync(1, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task LockUserAsync_AlreadyLocked_ThrowsBusinessException409()
    {
        // Arrange
        var admin = await SeedAdminUserAsync(99);
        var target = await SeedActiveUserAsync(1);
        target.Status = UserStatus.Locked;
        target.LockedAt = DateTimeOffset.UtcNow.AddHours(-1);
        target.LockedReason = "Previous lock";
        target.LockedBy = 99;
        target.DateLastMaint = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var request = new LockUserRequest { Reason = "New lock attempt" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.LockUserAsync(1, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.UserAlreadyLocked, ex.ResponseCode);
    }

    [Fact]
    public async Task LockUserAsync_EmptyReason_ThrowsBusinessException400()
    {
        // Arrange
        await SeedAdminUserAsync(99);
        await SeedActiveUserAsync(1);

        var request = new LockUserRequest { Reason = "   " };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.LockUserAsync(1, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task LockUserAsync_ReasonTooShort_ThrowsBusinessException400()
    {
        // Arrange
        await SeedAdminUserAsync(99);
        await SeedActiveUserAsync(1);

        var request = new LockUserRequest { Reason = "abc" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.LockUserAsync(1, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.BadRequest, ex.StatusCode);
        Assert.Equal(ResponseMessages.LockReasonTooShort, ex.Message);
    }

    #endregion

    #region LockUserAsync — Token Revocation

    [Fact]
    public async Task LockUserAsync_RevokesAllSessionsAndTokens()
    {
        // Arrange
        var admin = await SeedAdminUserAsync(99);
        var target = await SeedActiveUserAsync(1);

        // Create 3 sessions and 3 refresh tokens
        for (int i = 1; i <= 3; i++)
        {
            var session = new UserSession
            {
                Id = 100 + i,
                UserId = 1,
                SessionTokenHash = $"hash{i}",
                Status = SessionStatus.Active,
                LoginAt = DateTimeOffset.UtcNow,
                ExpiredAt = DateTimeOffset.UtcNow.AddDays(7),
                EffDate = DateTimeOffset.UtcNow,
                DateLastMaint = DateTimeOffset.UtcNow
            };
            _db.UserSessions.Add(session);
            await _db.SaveChangesAsync();

            var token = new RefreshToken
            {
                Id = 200 + i,
                UserId = 1,
                SessionId = session.Id,
                TokenHash = $"hash{i}",
                Status = TokenStatus.Active,
                IssuedAt = DateTimeOffset.UtcNow,
                ExpiredAt = DateTimeOffset.UtcNow.AddDays(7),
                EffDate = DateTimeOffset.UtcNow,
                DateLastMaint = DateTimeOffset.UtcNow
            };
            _db.RefreshTokens.Add(token);
            await _db.SaveChangesAsync();
        }

        var request = new LockUserRequest { Reason = "Revoke all sessions" };

        // Act
        await _sut.LockUserAsync(1, request, 99, null, null, null, CancellationToken.None);

        // Assert — DB-level verification (DTO no longer exposes revocation counts per spec A04)
        var activeSessions = await _db.UserSessions
            .Where(s => s.UserId == 1 && s.Status == SessionStatus.Active)
            .CountAsync();
        Assert.Equal(0, activeSessions);

        var activeTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == 1 && rt.Status == TokenStatus.Active)
            .CountAsync();
        Assert.Equal(0, activeTokens);
    }

    #endregion

    #region LockUserAsync — Audit Log

    [Fact]
    public async Task LockUserAsync_WritesStructuredAuditLog()
    {
        // Arrange
        var admin = await SeedAdminUserAsync(99);
        var target = await SeedActiveUserAsync(1);

        var request = new LockUserRequest { Reason = "Audit test" };

        // Act
        await _sut.LockUserAsync(1, request, 99, "10.0.0.1", "TestBrowser", "trace-audit-1", CancellationToken.None);

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal("USER_LOCK", _auditLog.LoggedAction);
        Assert.Equal(1, _auditLog.LoggedEntityId);
        Assert.Equal("User", _auditLog.LoggedEntityType);

        // Structured audit fields (new in A04)
        Assert.Equal("Active", _auditLog.LoggedOldValue);
        Assert.Equal("Locked", _auditLog.LoggedNewValue);
        Assert.Equal("trace-audit-1", _auditLog.LoggedTraceId);
        Assert.Contains("Audit test", _auditLog.LoggedDetails ?? "");
    }

    #endregion

    #region UnlockUserAsync — Success

    [Fact]
    public async Task UnlockUserAsync_LockedUser_SucceedsAndRestoresStatus()
    {
        // Arrange
        var admin = await SeedAdminUserAsync(99);
        var target = await SeedActiveUserAsync(1);
        target.Status = UserStatus.Locked;
        target.LockedAt = DateTimeOffset.UtcNow.AddHours(-1);
        target.LockedReason = "Previous violation";
        target.LockedBy = 99;
        target.DateLastMaint = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.UnlockUserAsync(
            targetUserId: 1,
            adminUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "Test Agent",
            traceId: "trace-unlock-1",
            CancellationToken.None);

        // Assert
        Assert.Equal("Active", result.Status);
        Assert.Null(result.LockedAt);
        Assert.Null(result.LockedReason);
        Assert.NotNull(result.LockedBy);

        var dbUser = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == 1);
        Assert.Equal(UserStatus.Active, dbUser.Status);
        Assert.Null(dbUser.LockedReason);
        Assert.Null(dbUser.LockedBy);
    }

    [Fact]
    public async Task UnlockUserAsync_RecordsStructuredAuditLog()
    {
        // Arrange
        var admin = await SeedAdminUserAsync(99);
        var target = await SeedActiveUserAsync(1);
        target.Status = UserStatus.Locked;
        target.LockedAt = DateTimeOffset.UtcNow;
        target.LockedReason = "X";
        target.LockedBy = 99;
        target.DateLastMaint = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        // Act
        await _sut.UnlockUserAsync(1, 99, "10.0.0.1", "TestBrowser", "trace-u-2", CancellationToken.None);

        // Assert
        Assert.True(_auditLog.LogCalled);
        Assert.Equal("USER_UNLOCK", _auditLog.LoggedAction);
        Assert.Equal("Locked", _auditLog.LoggedOldValue);
        Assert.Equal("Active", _auditLog.LoggedNewValue);
        Assert.Equal("trace-u-2", _auditLog.LoggedTraceId);
    }

    #endregion

    #region UnlockUserAsync — Failure

    [Fact]
    public async Task UnlockUserAsync_NotLocked_ThrowsBusinessException409()
    {
        // Arrange
        var admin = await SeedAdminUserAsync(99);
        await SeedActiveUserAsync(1);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UnlockUserAsync(1, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.UserNotLocked, ex.ResponseCode);
    }

    [Fact]
    public async Task UnlockUserAsync_UserNotFound_ThrowsBusinessException404()
    {
        // Arrange
        await SeedAdminUserAsync(99);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UnlockUserAsync(9999, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task UnlockUserAsync_DeletedUser_ThrowsBusinessException409()
    {
        // Arrange
        var admin = await SeedAdminUserAsync(99);
        var target = await SeedActiveUserAsync(1);
        target.Status = UserStatus.Locked;
        target.LockedAt = DateTimeOffset.UtcNow;
        target.DeletedAt = DateTimeOffset.UtcNow;
        target.DateLastMaint = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UnlockUserAsync(1, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
    }

    #endregion
}

/// <summary>
/// In-memory test double for IAuditLogService that captures logged entries, including
/// the new structured audit fields required by spec A04.
/// </summary>
internal sealed class TestAuditLogService : Core.Interfaces.Services.IAuditLogService
{
    public bool LogCalled { get; private set; }
    public string? LoggedAction { get; private set; }
    public long? LoggedEntityId { get; private set; }
    public string? LoggedEntityType { get; private set; }
    public string? LoggedDetails { get; private set; }
    public string? LoggedOldValue { get; private set; }
    public string? LoggedNewValue { get; private set; }
    public string? LoggedTraceId { get; private set; }

    public Task LogAsync(
        string action,
        long? userId = null,
        string? email = null,
        long? sessionId = null,
        long? deviceId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? entityType = null,
        long? entityId = null,
        string? details = null,
        string? oldValue = null,
        string? newValue = null,
        string? traceId = null,
        CancellationToken cancellationToken = default)
    {
        LogCalled = true;
        LoggedAction = action;
        LoggedEntityId = entityId;
        LoggedEntityType = entityType;
        LoggedDetails = details;
        LoggedOldValue = oldValue;
        LoggedNewValue = newValue;
        LoggedTraceId = traceId;
        return Task.CompletedTask;
    }
}
