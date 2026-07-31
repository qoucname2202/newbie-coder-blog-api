using Microsoft.AspNetCore.Mvc;
using NewbieCoder.API.Attributes;
using NewbieCoder.API.Extensions;
using NewbieCoder.Core.Constants;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Exceptions;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Controllers;

/// <summary>
/// Handles administrative category management operations: create, list, detail, update, change status, and delete.
/// </summary>
[ApiController]
[Route("api/v1/admin/categories")]
[Produces("application/json")]
[Tags("Admin — Categories")]
[RequiresRole(RoleConstants.Admin)]
public sealed class AdminCategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public AdminCategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    #region Create

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="request">Category creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created category data.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _categoryService.CreateCategoryAsync(
            request,
            createdByUserId: requesterId,
            ipAddress,
            userAgent,
            trace,
            cancellationToken);

        return StatusCode(
            HttpStatusCodes.Created,
            ApiResponse<CategoryResponse>.Success(
                result,
                trace,
                ResponseMessages.CategoryCreatedSuccess));
    }

    #endregion

    #region List

    /// <summary>
    /// Returns a paginated list of categories.
    /// </summary>
    /// <param name="filter">Search, filter, sort, and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<CategoryListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] CategoryFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _categoryService.GetCategoriesAsync(filter, cancellationToken);

        return Ok(ApiResponse<PaginatedResponse<CategoryListItemResponse>>.Success(
            result,
            trace,
            ResponseMessages.CategoryRetrievedSuccess));
    }

    #endregion

    #region GetById

    /// <summary>
    /// Returns full details of a category by ID.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{categoryId:long}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCategoryById(
        long categoryId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();

        var result = await _categoryService.GetCategoryByIdAsync(categoryId, cancellationToken);

        return Ok(ApiResponse<CategoryDetailResponse>.Success(
            result,
            trace,
            ResponseMessages.CategoryRetrievedSuccess));
    }

    #endregion

    #region Update

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="request">Category update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated category data.</returns>
    [HttpPut("{categoryId:long}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCategory(
        long categoryId,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _categoryService.UpdateCategoryAsync(
            categoryId,
            request,
            updatedByUserId: requesterId,
            ipAddress,
            userAgent,
            trace,
            cancellationToken);

        return Ok(ApiResponse<CategoryResponse>.Success(
            result,
            trace,
            ResponseMessages.CategoryUpdatedSuccess));
    }

    #endregion

    #region ChangeStatus

    /// <summary>
    /// Changes the status of a category (active / inactive).
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="request">Status change data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated category data.</returns>
    [HttpPatch("{categoryId:long}/status")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeCategoryStatus(
        long categoryId,
        [FromBody] ChangeCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        var result = await _categoryService.ChangeStatusAsync(
            categoryId,
            request,
            changedByUserId: requesterId,
            ipAddress,
            userAgent,
            trace,
            cancellationToken);

        return Ok(ApiResponse<CategoryResponse>.Success(
            result,
            trace,
            ResponseMessages.CategoryUpdatedSuccess));
    }

    #endregion

    #region Delete

    /// <summary>
    /// Soft-deletes a category. Fails if the category has child categories or associated posts.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{categoryId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCategory(
        long categoryId,
        CancellationToken cancellationToken)
    {
        var trace = HttpContext.GetRequestTrace();
        var requesterId = GetRequiredUserId();
        var ipAddress = GetClientIp();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        await _categoryService.DeleteCategoryAsync(
            categoryId,
            deletedByUserId: requesterId,
            ipAddress,
            userAgent,
            trace,
            cancellationToken);

        return NoContent();
    }

    #endregion

    #region Private Helpers

    private long GetRequiredUserId()
    {
        var userId = User.GetUserId();
        if (userId == null)
            throw new BusinessException(
                ResponseMessages.Unauthenticated,
                statusCode: HttpStatusCodes.Unauthorized,
                responseCode: ResponseCodes.Unauthorized);

        return userId.Value;
    }

    private string? GetClientIp()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    #endregion
}
