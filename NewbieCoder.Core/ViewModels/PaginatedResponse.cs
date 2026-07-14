namespace NewbieCoder.Core.ViewModels;

/// <summary>
/// Generic paginated response wrapper used by list endpoints that support pagination.
/// Follows the contract defined in docs/05-api-contracts.md.
/// </summary>
/// <typeparam name="T">The type of each item in the result set.</typeparam>
public sealed class PaginatedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public required int TotalPages { get; init; }
    public required bool HasNextPage { get; init; }
    public required bool HasPreviousPage { get; init; }
}
