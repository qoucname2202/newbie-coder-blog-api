# 09 — Coding Standards

C# and ASP.NET conventions. Complements [07-security-and-clean-code.md](07-security-and-clean-code.md).

---

## RESTful API

| Operation | HTTP status | responseCode |
|-----------|-------------|--------------|
| GET success | 200 | `000000` |
| POST create success | 201 | `000000` |
| PUT / PATCH success | 200 | `000000` |
| Validation error | 400 | `00000201` |
| Not authenticated | 401 | `00000204` |
| Forbidden | 403 | `00000205` |
| Not found | 404 | `00000202` |
| Rate limited | 429 | `00000429` |
| Server error | 500 | `00000500` |

Use `HttpStatusCodes` + `ResponseCodes` constants together.

**List GET:** always paginated — `PagingRequest` + `PagingDefaults.MaxPageSize`.

---

## Code comments (mandatory)

| Required | Example |
|----------|---------|
| Class summary | `/// <summary>Handles todo business rules.</summary>` |
| Complex methods | XML summary + inline `//` for non-obvious steps |
| Business rules | `// BR-001: title must not be empty` |
| Language | **Simple English only** |

Do not comment obvious code. Do comment complex algorithms, security checks, and paging math.

---

## Response envelope in controllers

```csharp
// GET single resource — 200 OK
return Ok(ApiResponse<TodoResponse>.Success(data, trace));

// POST create — 201 Created
return StatusCode(HttpStatusCodes.Created, ApiResponse<TodoResponse>.Success(created, trace));

// Errors — throw; middleware returns failure envelope
throw new BusinessException(
    "Todo not found.",
    statusCode: HttpStatusCodes.NotFound,
    responseCode: ResponseCodes.NotFound);
```

| Do | Don't |
|----|-------|
| `HttpStatusCodes` + `ResponseCodes` pairs | Mismatch HTTP 404 with `00000201` |
| Paginate all list GET | `GetAll()` without skip/take |
| English comments on complex logic | Vietnamese in source code |

---

## Security

- No secrets in code, git, logs, or responses
- Connection strings via `IConfiguration` only

---

## Clean code

- File-scoped namespaces, nullable enabled, primary constructors for DI
- Async: `Async` suffix + `CancellationToken`
- Thin controllers — delegate to service layer
- No empty catch blocks

---

## Input Validation

All data crossing the API boundary must be validated before any business logic runs. Validation belongs at the **request DTO** level — not in the controller or service.

### Rule 1 — Validate at the boundary

Every `Create*Request`, `Update*Request`, and `QueryRequest` DTO must have:

```csharp
public record CreatePostRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    [StringLength(50000, ErrorMessage = "Content must not exceed 50000 characters.")]
    public string Content { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a positive integer.")]
    public int CategoryId { get; init; }

    public List<int> TagIds { get; init; } = new();
}
```

Use `[ApiController]` on controllers — ASP.NET Core validates automatically before the action runs:

```csharp
[ApiController]  // ← enables automatic model validation
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    // If request is invalid, returns 400 BEFORE this action body executes
    public async Task<IActionResult> CreatePost(CreatePostRequest request, ...) { }
}
```

### Rule 2 — Required attribute for all mandatory fields

| Field type | Validation |
|------------|------------|
| Non-nullable string | `[Required]` + non-empty default |
| Nullable enum | `[Required]` + `AllowEmptyStrings = false` |
| Numeric IDs | `[Required]` + `[Range]` for positive values |
| Optional query params | Omit `[Required]`; use nullable type + null check |
| Date ranges | `[Range]` with `Minimum` / `Maximum` |

### Rule 3 — String length limits (defense in depth)

| Field | Max length | Why |
|-------|-----------|-----|
| Short text (title, name) | 200 | UI display, storage |
| Long text (description) | 2000 | Storage, indexing |
| Content / body | 50000 | Storage, performance |
| Email address | 254 | RFC 5321 |
| Password | 128 | Store as hash only |

Never rely solely on `[Required]` — always pair it with `[StringLength]`.

### Rule 4 — Numeric and ID range validation

```csharp
// ✅ Validate positive IDs (no zero or negative)
[Range(1, int.MaxValue, ErrorMessage = "Id must be greater than 0.")]

// ✅ Validate pagination bounds
public record PagingRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1.")]
    public int Page { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; init; } = 10;
}
```

### Rule 5 — No validation logic in controllers

Controllers must not contain if-checks for input validation:

```csharp
// ❌ Controller doing validation — violates thin controller rule
public async Task<IActionResult> CreatePost(CreatePostRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Title))
        return BadRequest("Title is required.");
    // ...
}

// ✅ Controller trusts model validation + throws BusinessException for domain rules
public async Task<IActionResult> CreatePost(CreatePostRequest request)
{
    var post = await _postService.CreateAsync(request, trace, ct);
    return StatusCode(HttpStatusCodes.Created, ApiResponse<PostResponse>.Success(post, trace));
}
```

### Rule 6 — Custom validation for cross-field rules

Use `IValidatableObject` for rules that involve multiple fields:

```csharp
public class DateRangeRequest : IValidatableObject
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && From > To)
        {
            yield return new ValidationResult(
                "From date must be earlier than To date.",
                new[] { nameof(From), nameof(To) });
        }
    }
}
```

### Rule 7 — Regex for structured strings

```csharp
// ✅ Validate slug format (e.g. "my-post-title")
[RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
    ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens.")]

// ✅ Validate email format (simple; real email validation done by data annotation)
[EmailAddress(ErrorMessage = "Invalid email address format.")]

// ✅ Validate URL format
[Url(ErrorMessage = "Invalid URL format.")]
```

### Rule 8 — Never trust client-supplied data

Even with `[Required]` and `[Range]`, always validate:

```csharp
// ✅ Re-validate at service layer for domain invariants
public async Task<Post> CreateAsync(CreatePostRequest request, ...)
{
    // Even though CategoryId has [Range(1, int.MaxValue)], verify it exists
    var categoryExists = await _categoryRepo.ExistsAsync(c => c.Id == request.CategoryId);
    if (!categoryExists)
        throw new BusinessException(
            "Category not found.",
            statusCode: HttpStatusCodes.NotFound,
            responseCode: ResponseCodes.NotFound);
}
```

### Validation checklist per DTO

- [ ] Every non-nullable string has both `[Required]` and `[StringLength]`
- [ ] All numeric IDs have `[Range(1, ...)]`
- [ ] All `[StringLength]` limits match database column sizes
- [ ] Custom rules use `IValidatableObject` or service-layer `BusinessException`
- [ ] Controller has `[ApiController]` attribute
- [ ] No manual `if (x == null)` checks in controllers for validated fields
- [ ] `dotnet test` passes

---

## Agent checklist

- [ ] REST + correct HTTP/error code pairing
- [ ] Pagination on list endpoints
- [ ] English comments on new classes/complex logic
- [ ] Standard response envelope
- [ ] `dotnet test` passes
