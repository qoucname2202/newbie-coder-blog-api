using System.Text.Json;
using System.Text.RegularExpressions;
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

public sealed class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IAuditLogService _auditLog;

    public CategoryService(
        AppDbContext db,
        ICategoryRepository categoryRepo,
        IAuditLogService auditLog)
    {
        _db = db;
        _categoryRepo = categoryRepo;
        _auditLog = auditLog;
    }

    #region CreateCategoryAsync

    public async Task<CategoryResponse> CreateCategoryAsync(
        CreateCategoryRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var trimmedName = request.Name!.Trim();
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(trimmedName)
            : request.Slug.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new BusinessException(
                ResponseMessages.CategoryNameRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CategoryNameRequired);

        if (await _categoryRepo.ExistsSlugAsync(slug, cancellationToken))
            throw new BusinessException(
                ResponseMessages.CategorySlugAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CategorySlugAlreadyExists);

        if (request.ParentId.HasValue)
        {
            var parent = await _categoryRepo.GetActiveByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent is null)
                throw new BusinessException(
                    ResponseMessages.CategoryNotFound,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.CategoryNotFound);
        }

        var category = new PostCategory
        {
            Name = trimmedName,
            Slug = slug,
            Description = request.Description?.Trim(),
            ParentId = request.ParentId,
            Status = EntityStatus.Active,
            EffDate = DateTimeOffset.UtcNow,
            DateLastMaint = DateTimeOffset.UtcNow
        };

        await _categoryRepo.AddAsync(category, cancellationToken);
        await _categoryRepo.SaveChangesAsync(cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CategoryCreated,
            createdByUserId,
            ipAddress,
            userAgent,
            entityId: category.Id,
            details: $"Category '{category.Name}' created.",
            newValue: SerializeCategory(category),
            traceId: traceId,
            cancellationToken: cancellationToken);

        return ToResponse(category);
    }

    #endregion

    #region GetCategoriesAsync

    public async Task<PaginatedResponse<CategoryListItemResponse>> GetCategoriesAsync(
        CategoryFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        return await _categoryRepo.GetPagedAsync(filter, cancellationToken);
    }

    #endregion

    #region GetCategoryByIdAsync

    public async Task<CategoryDetailResponse> GetCategoryByIdAsync(
        long categoryId,
        CancellationToken cancellationToken = default)
    {
        var detail = await _categoryRepo.GetDetailByIdAsync(categoryId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.CategoryNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CategoryNotFound);

        return detail;
    }

    #endregion

    #region UpdateCategoryAsync

    public async Task<CategoryResponse> UpdateCategoryAsync(
        long categoryId,
        UpdateCategoryRequest request,
        long updatedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetTrackedByIdAsync(categoryId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.CategoryNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CategoryNotFound);

        var trimmedName = request.Name!.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new BusinessException(
                ResponseMessages.CategoryNameRequired,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CategoryNameRequired);

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(trimmedName)
            : request.Slug.Trim().ToLowerInvariant();

        if (await _categoryRepo.ExistsSlugExcludingIdAsync(categoryId, slug, cancellationToken))
            throw new BusinessException(
                ResponseMessages.CategorySlugAlreadyExists,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CategorySlugAlreadyExists);

        if (request.ParentId.HasValue && request.ParentId.Value == categoryId)
            throw new BusinessException(
                "A category cannot be its own parent.",
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.ValidationError);

        if (request.ParentId.HasValue)
        {
            var parent = await _categoryRepo.GetActiveByIdAsync(request.ParentId.Value, cancellationToken);
            if (parent is null)
                throw new BusinessException(
                    ResponseMessages.CategoryNotFound,
                    statusCode: HttpStatusCodes.NotFound,
                    responseCode: ResponseCodes.CategoryNotFound);
        }

        var oldValue = SerializeCategory(category);

        category.Name = trimmedName;
        category.Slug = slug;
        category.Description = request.Description?.Trim();
        category.ParentId = request.ParentId;
        category.DateLastMaint = DateTimeOffset.UtcNow;

        if (request.IsActive.HasValue)
            category.Status = request.IsActive.Value ? EntityStatus.Active : EntityStatus.Inactive;

        await _categoryRepo.SaveChangesAsync(cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CategoryUpdated,
            updatedByUserId,
            ipAddress,
            userAgent,
            entityId: category.Id,
            details: $"Category '{category.Name}' updated.",
            oldValue: oldValue,
            newValue: SerializeCategory(category),
            traceId: traceId,
            cancellationToken: cancellationToken);

        return ToResponse(category);
    }

    #endregion

    #region ChangeStatusAsync

    public async Task<CategoryResponse> ChangeStatusAsync(
        long categoryId,
        ChangeCategoryStatusRequest request,
        long changedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetTrackedByIdAsync(categoryId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.CategoryNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CategoryNotFound);

        if (!Enum.TryParse<EntityStatus>(request.Status, ignoreCase: true, out var newStatus))
            throw new BusinessException(
                ResponseMessages.CategoryInvalidStatus,
                statusCode: HttpStatusCodes.BadRequest,
                responseCode: ResponseCodes.CategoryInvalidStatus);

        if (category.Status == newStatus)
            throw new BusinessException(
                ResponseMessages.CategoryAlreadyInTargetStatus,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CategoryAlreadyInTargetStatus);

        var oldStatus = category.Status;
        category.Status = newStatus;
        category.DateLastMaint = DateTimeOffset.UtcNow;

        await _categoryRepo.SaveChangesAsync(cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CategoryStatusChanged,
            changedByUserId,
            ipAddress,
            userAgent,
            entityId: category.Id,
            details: $"Category '{category.Name}' status changed from '{oldStatus}' to '{newStatus}'.",
            traceId: traceId,
            cancellationToken: cancellationToken);

        return ToResponse(category);
    }

    #endregion

    #region DeleteCategoryAsync

    public async Task DeleteCategoryAsync(
        long categoryId,
        long deletedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepo.GetTrackedByIdAsync(categoryId, cancellationToken)
            ?? throw new BusinessException(
                ResponseMessages.CategoryNotFound,
                statusCode: HttpStatusCodes.NotFound,
                responseCode: ResponseCodes.CategoryNotFound);

        if (await _categoryRepo.HasChildrenAsync(categoryId, cancellationToken))
            throw new BusinessException(
                ResponseMessages.CategoryHasChildren,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CategoryHasChildren);

        if (await _categoryRepo.HasPostsAsync(categoryId, cancellationToken))
            throw new BusinessException(
                ResponseMessages.CategoryHasPosts,
                statusCode: HttpStatusCodes.Conflict,
                responseCode: ResponseCodes.CategoryHasPosts);

        var oldValue = SerializeCategory(category);

        category.DeletedAt = DateTimeOffset.UtcNow;
        category.DeletedBy = deletedByUserId;
        category.Status = EntityStatus.Inactive;
        category.DateLastMaint = DateTimeOffset.UtcNow;

        await _categoryRepo.SaveChangesAsync(cancellationToken);

        await SafeAuditLogAsync(
            AuditActions.CategoryDeleted,
            deletedByUserId,
            ipAddress,
            userAgent,
            entityId: category.Id,
            details: $"Category '{category.Name}' deleted.",
            oldValue: oldValue,
            traceId: traceId,
            cancellationToken: cancellationToken);
    }

    #endregion

    #region Private Helpers

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Trim()
            .Replace(" ", "-");

        slug = Regex.Replace(slug, @"[^a-z0-9-]", "");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        if (slug.Length > 200)
            slug = slug[..200].Trim('-');

        return string.IsNullOrWhiteSpace(slug) ? "category" : slug;
    }

    private static CategoryResponse ToResponse(PostCategory category) =>
        new()
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            IsActive = category.Status == EntityStatus.Active,
            CreatedAt = category.EffDate,
            UpdatedAt = category.DateLastMaint
        };

    private static string SerializeCategory(PostCategory category) =>
        JsonSerializer.Serialize(category, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

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
                entityType: nameof(PostCategory),
                entityId: entityId,
                details: details,
                oldValue: oldValue,
                newValue: newValue,
                traceId: traceId,
                cancellationToken: cancellationToken);
        }
        catch { /* non-fatal */ }
    }

    #endregion
}
