using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Services;

public sealed class RoleService : IRoleService
{
    private readonly AppDbContext _db;
    private readonly IRoleRepository _roleRepo;
    private readonly IAuditLogService _auditLog;

    public RoleService(AppDbContext db, IRoleRepository roleRepo, IAuditLogService auditLog)
    {
        _db = db;
        _roleRepo = roleRepo;
        _auditLog = auditLog;
    }

    public async Task<RoleResponse> CreateRoleAsync(
        CreateRoleRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var trimmedName = request.Name!.Trim();

        // Business rule: name cannot be only whitespace
        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new BusinessException(
                ResponseMessages.ValidationError,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.ValidationError);

        // Business rule: check duplicate name
        if (await _roleRepo.ExistsByNameAsync(trimmedName, cancellationToken))
            throw new BusinessException(
                ResponseMessages.RoleNameAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.RoleNameAlreadyExists);

        var code = GenerateCode(trimmedName);

        Role? createdRole = null;

        await ExecuteInTransactionAsync(async () =>
        {
            createdRole = new Role
            {
                Code = code,
                Name = trimmedName,
                Description = request.Description?.Trim(),
                IsSystem = true,
                Status = RoleStatus.Active,
                EffDate = DateTimeOffset.UtcNow,
                DateLastMaint = DateTimeOffset.UtcNow
            };

            await _roleRepo.AddAsync(createdRole, cancellationToken);
            await _roleRepo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        // Audit log (non-fatal)
        await SafeAuditLogAsync(
            AuditActions.RoleCreated, createdByUserId, ipAddress, userAgent,
            entityId: createdRole!.Id,
            details: $"Role '{createdRole.Name}' (Code: {createdRole.Code}) created.",
            newValue: SerializeRole(createdRole),
            traceId: traceId,
            cancellationToken: cancellationToken);

        return new RoleResponse
        {
            Id = createdRole.Id,
            Code = createdRole.Code,
            Name = createdRole.Name,
            Description = createdRole.Description,
            IsSystemRole = createdRole.IsSystem,
            IsActive = createdRole.Status == RoleStatus.Active,
            CreatedAt = createdRole.EffDate
        };
    }

    public async Task<PaginatedResponse<RoleListItemResponse>> GetRolesAsync(
        RoleFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        return await _roleRepo.GetPagedAsync(filter, cancellationToken);
    }

    public async Task<RoleDetailResponse> GetRoleByIdAsync(
        long roleId,
        CancellationToken cancellationToken = default)
    {
        var detail = await _roleRepo.GetDetailByIdAsync(roleId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.RoleNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.RoleNotFound);

        return detail;
    }

    public async Task<RoleResponse> UpdateRoleAsync(
        long roleId,
        UpdateRoleRequest request,
        long updatedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleRepo.GetByIdAsync(roleId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.RoleNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.RoleNotFound);

        // Business rule: system roles cannot be modified
        if (role.IsSystem)
            throw new BusinessException(
                ResponseMessages.SystemRoleCannotBeModified,
                statusCode: HttpStatusCodes.Forbidden,
                responseCode: ResponseCodes.SystemRoleCannotBeModified);

        var trimmedName = request.Name!.Trim();

        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new BusinessException(
                ResponseMessages.ValidationError,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.ValidationError);

        // Check duplicate name (excluding this role)
        if (await _roleRepo.ExistsByNameExcludingIdAsync(roleId, trimmedName, cancellationToken))
            throw new BusinessException(
                ResponseMessages.RoleNameAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.RoleNameAlreadyExists);

        // Business rule: cannot deactivate the last admin role
        if (request.IsActive == false && role.Code == RoleConstants.Admin)
            throw new BusinessException(
                ResponseMessages.AdminRoleCannotBeDeactivated,
                statusCode: HttpStatusCodes.Forbidden,
                responseCode: ResponseCodes.AdminRoleCannotBeDeactivated);

        var oldData = SerializeRole(role);

        // Apply updates
        role.Name = trimmedName;
        role.Description = request.Description?.Trim();
        role.DateLastMaint = DateTimeOffset.UtcNow;

        if (request.IsActive.HasValue)
            role.Status = request.IsActive.Value ? RoleStatus.Active : RoleStatus.Inactive;

        var newData = SerializeRole(role);

        await ExecuteInTransactionAsync(async () =>
        {
            await _roleRepo.UpdateAsync(role, cancellationToken);
            await _roleRepo.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        // Audit log (non-fatal)
        await SafeAuditLogAsync(AuditActions.RoleUpdated, updatedByUserId, ipAddress, userAgent,
            entityId: role.Id,
            details: $"Role '{role.Name}' updated.",
            oldValue: oldData,
            newValue: newData,
            traceId: traceId,
            cancellationToken: cancellationToken);

        return new RoleResponse
        {
            Id = role.Id,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystem,
            IsActive = role.Status == RoleStatus.Active,
            CreatedAt = role.EffDate,
            UpdatedAt = role.DateLastMaint
        };
    }

    public async Task DeleteRoleAsync(
        long roleId,
        long deletedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleRepo.GetByIdAsync(roleId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.RoleNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.RoleNotFound);

        // Business rule: system roles cannot be deleted
        if (role.IsSystem)
            throw new BusinessException(
                ResponseMessages.SystemRoleCannotBeDeleted,
                statusCode: HttpStatusCodes.Forbidden,
                responseCode: ResponseCodes.SystemRoleCannotBeDeleted);

        // Business rule: cannot delete role that has assigned users
        if (await _roleRepo.HasAssignedUsersAsync(roleId, cancellationToken))
            throw new BusinessException(
                ResponseMessages.RoleIsAssignedToUsers,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.RoleIsAssignedToUsers);

        var oldData = SerializeRole(role);

        await ExecuteInTransactionAsync(async () =>
        {
            await _roleRepo.SoftDeleteAsync(role, deletedByUserId, cancellationToken);
        }, cancellationToken);

        // Audit log (non-fatal)
        await SafeAuditLogAsync(AuditActions.RoleDeleted, deletedByUserId, ipAddress, userAgent,
            entityId: role.Id,
            details: $"Role '{role.Name}' (Code: {role.Code}) soft-deleted.",
            oldValue: oldData,
            traceId: traceId,
            cancellationToken: cancellationToken);
    }

    #region Helpers

    private static string GenerateCode(string name)
    {
        var code = Regex.Replace(name.Trim().ToUpperInvariant(), @"[^a-zA-Z0-9]", "_");
        code = Regex.Replace(code, @"_+", "_").Trim('_');
        return string.IsNullOrEmpty(code) ? "CUSTOM_ROLE" : code;
    }

    private async Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private async Task SafeAuditLogAsync(
        string action,
        long? userId,
        string? ipAddress,
        string? userAgent,
        long? entityId,
        string? details,
        string? oldValue = null,
        string? newValue = null,
        string? traceId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _auditLog.LogAsync(
                action: action,
                userId: userId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                entityType: nameof(Role),
                entityId: entityId,
                details: details,
                oldValue: oldValue,
                newValue: newValue,
                traceId: traceId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // Audit log failures must not fail the primary operation.
        }
    }

    private static string SerializeRole(Role role) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            role.Id,
            role.Code,
            role.Name,
            role.Description,
            role.IsSystem,
            Status = role.Status.ToString()
        });

    #endregion
}
