namespace NewbieCoder.Core.DTOs.Response.Admin;

/// <summary>
/// Minimal author info embedded in post responses.
/// </summary>
public sealed class AuthorSummaryResponse
{
    public long Id { get; init; }
    public string Username { get; init; } = null!;
    public string FullName { get; init; } = null!;
}

/// <summary>
/// Minimal category info embedded in post responses.
/// </summary>
public sealed class CategorySummaryResponse
{
    public long Id { get; init; }
    public string Name { get; init; } = null!;
}

/// <summary>
/// Minimal tag info embedded in post responses.
/// </summary>
public sealed class TagSummaryResponse
{
    public long Id { get; init; }
    public string Name { get; init; } = null!;
}
