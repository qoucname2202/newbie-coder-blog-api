using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.Repositories;
using NewbieCoder.Infrastructure.Services;

namespace NewbieCoder.UnitTest.CoreTests;

public class RoleServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly RoleRepository _roleRepo;
    private readonly TestAuditLogService _auditLog;
    private readonly RoleService _sut;

    public RoleServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _roleRepo = new RoleRepository(_db);
        _auditLog = new TestAuditLogService();
        _sut = new RoleService(_db, _roleRepo, _auditLog);
    }

    public void Dispose() => _db.Dispose();

    #region Seed helpers

    private async Task<Role> SeedRoleAsync(
        long id = 1,
        string code = "ADMIN",
        string name = "Admin",
        string? description = null,
        bool isSystem = false,
        RoleStatus status = RoleStatus.Active)
    {
        var role = new Role
        {
            Id = id,
            Code = code,
            Name = name,
            Description = description,
            IsSystem = isSystem,
            Status = status,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return role;
    }

    private async Task SeedUserWithRoleAsync(long userId, long roleId, long assignedBy = 99)
    {
        var user = new User
        {
            Id = userId,
            Email = $"user{userId}@test.com",
            Username = $"user{userId}",
            Password = "hash",
            FullName = $"User {userId}",
            Location = "test",
            Status = UserStatus.Active,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedBy = assignedBy,
            Status = UserRoleStatus.Active,
            AssignedAt = DateTimeOffset.UtcNow,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };
        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync();
    }

    #endregion

    #region CreateRoleAsync — Success

    [Fact]
    public async Task CreateRoleAsync_ValidRequest_ReturnsRoleResponse()
    {
        // Arrange
        var request = new CreateRoleRequest
        {
            Name = "Moderator",
            Description = "Manages user content"
        };

        // Act
        var result = await _sut.CreateRoleAsync(
            request,
            createdByUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "TestAgent",
            traceId: "trace-1",
            CancellationToken.None);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("MODERATOR", result.Code);
        Assert.Equal("Moderator", result.Name);
        Assert.Equal("Manages user content", result.Description);
        Assert.True(result.IsSystemRole);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateRoleAsync_TrimsWhitespaceFromName()
    {
        var request = new CreateRoleRequest { Name = "  Editor  " };

        var result = await _sut.CreateRoleAsync(
            request, 99, null, null, null, CancellationToken.None);

        Assert.Equal("EDITOR", result.Code);
        Assert.Equal("Editor", result.Name);
    }

    [Fact]
    public async Task CreateRoleAsync_WritesAuditLog()
    {
        var request = new CreateRoleRequest { Name = "CustomRole" };

        await _sut.CreateRoleAsync(
            request, 99, "10.0.0.1", "Browser", "trace-2", CancellationToken.None);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.RoleCreated, _auditLog.LoggedAction);
        Assert.Equal("Role", _auditLog.LoggedEntityType);
        Assert.Contains("CustomRole", _auditLog.LoggedDetails ?? "");
    }

    #endregion

    #region CreateRoleAsync — Validation Failures

    [Fact]
    public async Task CreateRoleAsync_WhitespaceOnlyName_ThrowsBusinessException400()
    {
        var request = new CreateRoleRequest { Name = "   " };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreateRoleAsync(request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task CreateRoleAsync_DuplicateName_ThrowsBusinessException409()
    {
        await SeedRoleAsync(name: "Editor");

        var request = new CreateRoleRequest { Name = "Editor" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreateRoleAsync(request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.RoleNameAlreadyExists, ex.ResponseCode);
    }

    [Fact]
    public async Task CreateRoleAsync_CaseInsensitiveDuplicate_ThrowsBusinessException409()
    {
        await SeedRoleAsync(name: "Moderator");

        var request = new CreateRoleRequest { Name = "MODERATOR" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.CreateRoleAsync(request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task CreateRoleAsync_SpecialCharsInName_GeneratesValidCode()
    {
        var request = new CreateRoleRequest { Name = "Content Manager 2024!" };

        var result = await _sut.CreateRoleAsync(
            request, 99, null, null, null, CancellationToken.None);

        Assert.Equal("CONTENT_MANAGER_2024", result.Code);
    }

    #endregion

    #region GetRolesAsync — Success

    [Fact]
    public async Task GetRolesAsync_DefaultFilter_ReturnsPagedRoles()
    {
        await SeedRoleAsync(1, "ADMIN", "Admin", isSystem: true);
        await SeedRoleAsync(2, "USER", "User", isSystem: true);
        await SeedRoleAsync(3, "MOD", "Moderator", status: RoleStatus.Inactive);

        var filter = new RoleFilterRequest { Page = 1, PageSize = 10 };

        var result = await _sut.GetRolesAsync(filter, CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetRolesAsync_ByKeyword_ReturnsMatchingRoles()
    {
        await SeedRoleAsync(1, "ADMIN", "Administrator");
        await SeedRoleAsync(2, "USER", "Regular User");
        await SeedRoleAsync(3, "MOD", "Moderator");

        var filter = new RoleFilterRequest { Keyword = "admin" };

        var result = await _sut.GetRolesAsync(filter, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Administrator", result.Items[0].Name);
    }

    [Fact]
    public async Task GetRolesAsync_ByIsActiveFilter_ReturnsMatchingRoles()
    {
        await SeedRoleAsync(1, "A", "Active Role", status: RoleStatus.Active);
        await SeedRoleAsync(2, "I", "Inactive Role", status: RoleStatus.Inactive);

        var filter = new RoleFilterRequest { IsActive = true };

        var result = await _sut.GetRolesAsync(filter, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.True(result.Items[0].IsActive);
    }

    [Fact]
    public async Task GetRolesAsync_ByIsSystemRole_ReturnsMatchingRoles()
    {
        await SeedRoleAsync(1, "SYS", "System Role", isSystem: true);
        await SeedRoleAsync(2, "CUS", "Custom Role", isSystem: false);

        var filter = new RoleFilterRequest { IsSystemRole = true };

        var result = await _sut.GetRolesAsync(filter, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.True(result.Items[0].IsSystemRole);
    }

    [Fact]
    public async Task GetRolesAsync_ExcludesSoftDeletedRoles()
    {
        var role = await SeedRoleAsync(1, "DEL", "Deleted Role");
        role.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var result = await _sut.GetRolesAsync(new RoleFilterRequest(), CancellationToken.None);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetRolesAsync_Pagination_Works()
    {
        for (int i = 1; i <= 5; i++)
            await SeedRoleAsync(i, $"R{i}", $"Role {i}");

        var filter = new RoleFilterRequest { Page = 2, PageSize = 2 };

        var result = await _sut.GetRolesAsync(filter, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    #endregion

    #region GetRoleByIdAsync — Success & Not Found

    [Fact]
    public async Task GetRoleByIdAsync_ExistingRole_ReturnsDetail()
    {
        var role = await SeedRoleAsync(1, "ADMIN", "Administrator", "Full admin access", isSystem: true);

        var result = await _sut.GetRoleByIdAsync(1, CancellationToken.None);

        Assert.Equal(1, result.Id);
        Assert.Equal("ADMIN", result.Code);
        Assert.Equal("Administrator", result.Name);
        Assert.Equal("Full admin access", result.Description);
        Assert.True(result.IsSystemRole);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetRoleByIdAsync_NonExistentRole_ThrowsBusinessException404()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.GetRoleByIdAsync(9999, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
        Assert.Equal(ResponseCodes.RoleNotFound, ex.ResponseCode);
    }

    #endregion

    #region UpdateRoleAsync — Success

    [Fact]
    public async Task UpdateRoleAsync_ValidRequest_UpdatesRole()
    {
        await SeedRoleAsync(1, "MOD", "Moderator");

        var request = new UpdateRoleRequest
        {
            Name = "Senior Moderator",
            Description = "Senior mod role"
        };

        var result = await _sut.UpdateRoleAsync(
            1, request, updatedByUserId: 99,
            ipAddress: "127.0.0.1", userAgent: "Test", traceId: "trace-u",
            CancellationToken.None);

        Assert.Equal("Senior Moderator", result.Name);
        Assert.Equal("Senior mod role", result.Description);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateRoleAsync_UpdateIsActive_UpdatesStatus()
    {
        await SeedRoleAsync(1, "MOD", "Moderator", status: RoleStatus.Active);

        var request = new UpdateRoleRequest { Name = "Moderator", IsActive = false };

        var result = await _sut.UpdateRoleAsync(
            1, request, 99, null, null, null, CancellationToken.None);

        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateRoleAsync_WritesAuditLog()
    {
        await SeedRoleAsync(1, "TEST", "TestRole");

        var request = new UpdateRoleRequest { Name = "UpdatedRole" };

        await _sut.UpdateRoleAsync(1, request, 99, "10.0.0.1", "Browser", "trace-u2", CancellationToken.None);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.RoleUpdated, _auditLog.LoggedAction);
        Assert.Contains("UpdatedRole", _auditLog.LoggedDetails ?? "");
    }

    #endregion

    #region UpdateRoleAsync — Validation Failures

    [Fact]
    public async Task UpdateRoleAsync_NonExistentRole_ThrowsBusinessException404()
    {
        var request = new UpdateRoleRequest { Name = "Test" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdateRoleAsync(9999, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateRoleAsync_SystemRole_ThrowsBusinessException403()
    {
        await SeedRoleAsync(1, "ADMIN", "Admin", isSystem: true);

        var request = new UpdateRoleRequest { Name = "Changed Admin" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdateRoleAsync(1, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Forbidden, ex.StatusCode);
        Assert.Equal(ResponseCodes.SystemRoleCannotBeModified, ex.ResponseCode);
    }

    [Fact]
    public async Task UpdateRoleAsync_DuplicateName_ThrowsBusinessException409()
    {
        await SeedRoleAsync(1, "R1", "Role One");
        await SeedRoleAsync(2, "R2", "Role Two");

        var request = new UpdateRoleRequest { Name = "Role One" };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdateRoleAsync(2, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.RoleNameAlreadyExists, ex.ResponseCode);
    }

    [Fact]
    public async Task UpdateRoleAsync_AdminRole_DeactivateAdmin_ThrowsBusinessException403()
    {
        await SeedRoleAsync(1, RoleConstants.Admin, "Admin", isSystem: true);

        var request = new UpdateRoleRequest
        {
            Name = "Admin",
            IsActive = false
        };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdateRoleAsync(1, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Forbidden, ex.StatusCode);
        Assert.Equal(ResponseCodes.SystemRoleCannotBeModified, ex.ResponseCode);
    }

    [Fact]
    public async Task UpdateRoleAsync_WhitespaceName_ThrowsBusinessException400()
    {
        await SeedRoleAsync(1, "R1", "Role One", isSystem: false);

        var request = new UpdateRoleRequest { Name = "   " };

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdateRoleAsync(1, request, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.BadRequest, ex.StatusCode);
    }

    #endregion

    #region DeleteRoleAsync — Success

    [Fact]
    public async Task DeleteRoleAsync_CustomRole_SoftDeletes()
    {
        var role = await SeedRoleAsync(1, "CUSTOM", "Custom Role", isSystem: false);

        await _sut.DeleteRoleAsync(
            1,
            deletedByUserId: 99,
            ipAddress: "127.0.0.1",
            userAgent: "Test",
            traceId: "trace-d",
            CancellationToken.None);

        var dbRole = await _db.Roles.AsNoTracking().FirstAsync(r => r.Id == 1);
        Assert.NotNull(dbRole.DeletedAt);
        Assert.Equal(RoleStatus.Inactive, dbRole.Status);
    }

    [Fact]
    public async Task DeleteRoleAsync_WritesAuditLog()
    {
        await SeedRoleAsync(1, "CUSTOM", "Custom Role", isSystem: false);

        await _sut.DeleteRoleAsync(1, 99, "10.0.0.1", "Browser", "trace-d2", CancellationToken.None);

        Assert.True(_auditLog.LogCalled);
        Assert.Equal(AuditActions.RoleDeleted, _auditLog.LoggedAction);
        Assert.Contains("Custom Role", _auditLog.LoggedDetails ?? "");
    }

    #endregion

    #region DeleteRoleAsync — Validation Failures

    [Fact]
    public async Task DeleteRoleAsync_NonExistentRole_ThrowsBusinessException404()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.DeleteRoleAsync(9999, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteRoleAsync_SystemRole_ThrowsBusinessException403()
    {
        await SeedRoleAsync(1, "ADMIN", "Admin", isSystem: true);

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.DeleteRoleAsync(1, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Forbidden, ex.StatusCode);
        Assert.Equal(ResponseCodes.SystemRoleCannotBeDeleted, ex.ResponseCode);
    }

    [Fact]
    public async Task DeleteRoleAsync_RoleWithAssignedUsers_ThrowsBusinessException409()
    {
        await SeedRoleAsync(1, "MOD", "Moderator", isSystem: false);
        await SeedUserWithRoleAsync(userId: 10, roleId: 1);

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.DeleteRoleAsync(1, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.Conflict, ex.StatusCode);
        Assert.Equal(ResponseCodes.RoleIsAssignedToUsers, ex.ResponseCode);
    }

    [Fact]
    public async Task DeleteRoleAsync_AlreadyDeletedRole_ThrowsBusinessException404()
    {
        var role = await SeedRoleAsync(1, "CUSTOM", "Custom", isSystem: false);
        role.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.DeleteRoleAsync(1, 99, null, null, null, CancellationToken.None));

        Assert.Equal(HttpStatusCodes.NotFound, ex.StatusCode);
    }

    #endregion

    #region UserCount in List Items

    [Fact]
    public async Task GetRolesAsync_IncludesUserCount()
    {
        var role = await SeedRoleAsync(1, "MOD", "Moderator", isSystem: false);
        await SeedUserWithRoleAsync(userId: 10, roleId: 1);
        await SeedUserWithRoleAsync(userId: 11, roleId: 1);
        await SeedUserWithRoleAsync(userId: 12, roleId: 1);

        var result = await _sut.GetRolesAsync(new RoleFilterRequest(), CancellationToken.None);

        Assert.Equal(3, result.Items[0].UserCount);
    }

    #endregion
}

