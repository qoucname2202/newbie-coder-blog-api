using System.ComponentModel.DataAnnotations;

namespace NewbieCoder.Core.DTOs.Request.Admin;

/// <summary>
/// Query parameters for GET /api/admin/users — supports search, filter, sort, and pagination.
/// Follows the contract defined in docs/05-api-contracts.md.
/// </summary>
public sealed class UserFilterRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than or equal to 1.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "pageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 10;

    [MaxLength(200, ErrorMessage = "keyword must not exceed 200 characters.")]
    public string? Keyword { get; set; }

    public string? Status { get; set; }

    [MaxLength(50, ErrorMessage = "sortBy must not exceed 50 characters.")]
    public string? SortBy { get; set; }

    [RegularExpression("^(asc|desc)$", ErrorMessage = "sortDirection must be 'asc' or 'desc'.")]
    public string SortDirection { get; set; } = "desc";
}
