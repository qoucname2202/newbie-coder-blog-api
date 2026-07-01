# 12 — Swagger API Documentation

Documentation rules for OpenAPI/Swagger in Newbie Coder API. Follow these rules **after code is written and `dotnet test` passes**. Swagger is only available in Development (see [11-decision-log.md](11-decision-log.md) ADR-007).

---

## 1. Why document APIs in Swagger

Swagger UI serves as the **interactive contract** for frontend developers, QA, and other teams. An undocumented endpoint is effectively hidden from collaborators. All public endpoints (except health checks) must be documented before a feature is considered complete.

---

## 2. When to add / update Swagger descriptions

| Situation | Action |
|-----------|--------|
| New endpoint added | Add `[HttpGet/Post/Put/Delete/...]` with full XML docs |
| Request DTO changed | Update `[FromBody]` summary and example |
| Response type changed | Update `ProducesResponseType` |
| Feature is feature-flagged | Only document after the flag is enabled |
| `dotnet test` fails | Do not document — fix the test first |
| Security concern (auth, rate limit) | Always document auth requirements |

---

## 3. Required attributes per endpoint

Every controller action **must** have these Swagger annotations:

```csharp
/// <summary>
/// Retrieves a paginated list of posts filtered by tag.
/// </summary>
/// <param name="tagId">The tag identifier to filter by (optional).</param>
/// <param name="paging">Pagination parameters (page, pageSize).</param>
/// <param name="cancellationToken">Propagates cancellation requests.</param>
/// <response code="200">Returns the paginated post list.</response>
/// <response code="400">Validation error — invalid paging parameters.</response>
/// <response code="401">Not authenticated.</response>
/// <response code="429">Rate limit exceeded.</response>
[HttpGet]
[ProducesResponseType(typeof(ApiResponse<PagedResult<PostResponse>>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status429TooManyRequests)]
[Authorize]
public async Task<IActionResult> GetPostsByTag(
    [FromQuery] int? tagId,
    [FromQuery] PagingRequest paging,
    CancellationToken cancellationToken)
```

### Minimum required annotations

| Annotation | Required on |
|------------|-------------|
| `/// <summary>` | Every `public` controller action |
| `/// <param>` | Every non-primitive parameter (`cancellationToken` included) |
| `[ProducesResponseType]` | Every action — at minimum the success code |
| `[Authorize]` / `[AllowAnonymous]` | Every action that requires (or skips) auth |

---

## 4. Documenting DTOs (request / response)

### Request DTOs — annotate with `[FromBody]` / `[FromQuery]` summary

```csharp
/// <summary>
/// Request payload for creating a new post.
/// </summary>
public record CreatePostRequest
{
    /// <summary>
    /// The post title. Required, max 200 characters.
    /// </summary>
    /// <example>Getting Started with ASP.NET Core</example>
    [Required]
    [StringLength(200)]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Markdown or plain-text content of the post.
    /// </summary>
    /// <example>## Introduction\n\nThis is my first post...</example>
    [Required]
    [StringLength(50000)]
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Category ID to assign the post to.
    /// </summary>
    /// <example>3</example>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    /// <summary>
    /// List of tag IDs to associate with this post.
    /// </summary>
    /// <example>[1, 4, 7]</example>
    public List<int> TagIds { get; init; } = new();
}
```

### Response DTOs — document nested structures

```csharp
/// <summary>
/// Represents a post in list/detail responses.
/// </summary>
public record PostResponse
{
    /// <summary>Unique identifier of the post.</summary>
    /// <example>42</example>
    public int Id { get; init; }

    /// <summary>The post title.</summary>
    /// <example>Getting Started with ASP.NET Core</example>
    public string Title { get; init; } = string.Empty;

    /// <summary>Author's display name.</summary>
    /// <example>John Doe</example>
    public string AuthorName { get; init; } = string.Empty;

    /// <summary>Number of published comments.</summary>
    /// <example>7</example>
    public int CommentCount { get; init; }

    /// <summary>Tags assigned to this post.</summary>
    public List<TagSummary> Tags { get; init; } = new();
}
```

---

## 5. XML documentation comments — rules

| Rule | Reason |
|------|--------|
| Every `[FromQuery]` parameter needs a `<param name="...">` entry | Makes Swagger UI show parameter descriptions |
| Primitive params need `[Required]` or `[Range]` on the DTO, not just `[param]` | Validation and Swagger display |
| Use `<example>` for non-trivial fields | Gives callers a clear starting value |
| Keep summaries **≤ 1 sentence**; descriptions only for complex params | Keeps Swagger UI readable |
| Do not repeat the HTTP method in the summary (don't say "GET the list...") | Already visible in Swagger UI |
| Simple `bool` / `int` params: one-line `<param>` is enough | Over-documentation noise |

---

## 6. Grouping and tagging

Use `[Tags]` to group endpoints by resource:

```csharp
[ApiController]
[Route("api/[controller]")]
[Tags("Posts")]
public class PostsController : ControllerBase { }
```

**Tag names:** use **plural nouns** matching the route — `Posts`, `Categories`, `Tags`, `Auth`.

---

## 7. Authentication / authorization display

Swagger UI must show auth requirements clearly:

```csharp
[HttpPost("login")]
[AllowAnonymous] // No auth required to log in
[ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> Login([FromBody] LoginRequest request, ...)
{
    // ...
}

[HttpPost]
[Authorize(Roles = "Admin")] // Admin only
[ProducesResponseType(typeof(ApiResponse<PostResponse>), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
public async Task<IActionResult> CreatePost(...)
```

With JWT Bearer configured, Swagger UI will show an **"Authorize" button** and let testers paste a token.

---

## 8. Enum values in Swagger

Always show enum **names and values** in Swagger UI:

```csharp
public record PostResponse
{
    // ✅ Shown as dropdown in Swagger UI
    public PostStatus Status { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PostStatus
{
    Draft,
    Published,
    Archived
}
```

Do not rely on integer enum values alone — always use `JsonStringEnumConverter`.

---

## 9. Enabling detailed Swagger output

In `Program.cs`, configure full OpenAPI metadata:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Version = "v1",
            Title = "Newbie Coder API",
            Description = "Blog API for the Newbie Coder learning platform. " +
                          "All responses follow the standard ApiResponse<T> envelope.",
            Contact = new OpenApiContact { Name = "Newbie Coder Team" }
        });

    // Include XML comments for rich Swagger UI
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});
```

---

## 10. Agent checklist (every feature PR)

- [ ] Every new public endpoint has `/// <summary>` and all `[ProducesResponseType]`
- [ ] All non-primitive parameters have `<param>` entries
- [ ] Auth-decorated endpoints show `[Authorize]` or `[AllowAnonymous]`
- [ ] Enums use `JsonStringEnumConverter` (string values in Swagger)
- [ ] Example values (`<example>`) provided for non-trivial fields
- [ ] Tags match the controller route (plural, e.g. `Posts`)
- [ ] `dotnet test` passes before documentation is considered complete
- [ ] XML doc file is generated (`<GenerateDocumentationFile>true</GenerateDocumentationFile>` in `.csproj`)

---

## 11. Related documents

- [09-coding-standards.md](09-coding-standards.md) — REST status codes and response envelope
- [07-security-and-clean-code.md](07-security-and-clean-code.md) — security rules and auth requirements
- [05-api-contracts.md](05-api-contracts.md) — full HTTP contract and response format
- [11-decision-log.md](11-decision-log.md) — ADR-007: Swagger enabled in Development only
