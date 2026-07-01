# 07 — Security, Clean Code & Anti-Smell Rules

This document replaces the former database doc. **EF Core / migration details** live in `.github/instructions/infrastructure.instructions.md` and [04-solution-architecture.md](04-solution-architecture.md). This file defines **clean code**, **anti-hardcoding**, **code smell prevention**, and **security** rules all agents and developers must follow.

---

## 1. Clean code principles

### Single Responsibility (SRP)

| Layer | Responsibility | Must NOT do |
|-------|----------------|-------------|
| Controller | HTTP in/out, call service | Business rules, EF queries |
| Service (planned) | Business logic, orchestration | HTTP status mapping details |
| Repository | Data access staging | `SaveChanges`, validation |
| Middleware | Cross-cutting HTTP concerns | Domain rules |

One class = one reason to change.

### DRY (Don't Repeat Yourself)

| Do | Don't |
|----|-------|
| Reuse `ApiResponse<T>.Success()` / `.Fail()` | Build anonymous JSON objects in controllers |
| Centralize codes in `ResponseCodes` | Scatter `"00000201"` as string literals |
| Use `GetRequestTrace()` extension | Copy `Guid.NewGuid()` logic everywhere |
| Extract shared validation to service | Duplicate validation in every controller action |

### KISS (Keep It Simple)

- Prefer the existing generic `Repository<T>` before creating entity-specific repos unless queries are complex.
- Do not add abstraction layers (factories, builders) unless used in **3+ places**.
- Avoid “helper” methods that wrap a single line without adding clarity.

### Meaningful naming

```csharp
// ✅ Clear intent
var requestTrace = httpContext.GetRequestTrace();
throw new BusinessException("Title is required.", responseCode: ResponseCodes.ValidationError);

// ❌ Smell — cryptic or misleading
var t = Guid.NewGuid();
throw new BusinessException("err");
```

---

## 2. Anti-hardcoding rules

### Response codes and messages

All response codes and default messages **must** come from constants:

| Constant class | Location | Example |
|----------------|----------|---------|
| `ResponseCodes` | `Core/Constants/ResponseCodes.cs` | `000000`, `00000201` |
| `ResponseMessages` | `Core/Constants/ResponseMessages.cs` | `"Success"` |
| `AppConstants` | `Core/Constants/AppConstants.cs` | API title, version |

```csharp
// ✅
ResponseCode = ResponseCodes.Success

// ❌ FORBIDDEN in production code
ResponseCode = "000000"
```

When adding a new business error domain, add a new constant to `ResponseCodes` with a comment describing when to use it.

### Configuration and secrets

| Value | Where it belongs | Never |
|-------|------------------|-------|
| Connection strings | `appsettings*.json`, env vars, User Secrets | Hardcoded in `.cs` files |
| API keys, JWT secrets | Environment / Key Vault / User Secrets | Committed to git |
| Docker SA password | `docker-compose.yml` (dev only) | Production deployment |
| Ports / URLs | `launchSettings.json`, config | Magic numbers in code |

Use `IConfiguration` or options pattern:

```csharp
// ✅
configuration.GetConnectionString("DefaultConnection")

// ❌
var conn = "Server=...;Password=MySecret123";
```

### Magic strings for HttpContext

Use `HttpContextItemKeys.RequestTrace` — not `"RequestTrace"` inline.

### Dates and timezones

Use `DateTimeOffset` and `ApiResponse.FormatDateTime()` — do not hand-format date strings in controllers.

---

## 3. Code smell catalog — detect and fix

| Smell | Symptom | Fix |
|-------|---------|-----|
| **Fat controller** | Controller > ~30 lines, has LINQ/EF | Move to service |
| **God class** | One class does everything | Split by responsibility |
| **Primitive obsession** | Raw `string` for codes, traces | Use constants / value objects |
| **Feature envy** | Controller reads many `HttpContext` items | Use accessor / middleware |
| **Shotgun surgery** | One change touches 10 files for response format | Use shared envelope (already in place) |
| **Leaking abstraction** | `DbContext` in API layer | Inject repository/service only |
| **Exception swallowing** | Empty `catch { }` | Log and rethrow or use middleware |
| **Async blocking** | `.Result`, `.Wait()` | `await` end-to-end |
| **Boolean flag params** | `DoWork(true, false, true)` | Named parameters or separate methods |
| **Comment-driven code** | Comments explain what code should do | Refactor for clarity |

Agents: if you introduce any smell above, refactor before completing the task.

---

## 4. Security rules — secrets and data exposure

### 4.1 Never commit secrets

**Forbidden in git:**

- `.env` with real credentials
- `appsettings.Production.json` with secrets (use env vars)
- API keys, JWT signing keys, connection strings with production passwords
- Private certificates

**Required:**

- `.gitignore` must cover User Secrets, `.env`, local overrides
- Use `dotnet user-secrets` for local dev sensitive values
- Use `ConnectionStrings__DefaultConnection` env var in Docker/CI/production

### 4.2 Never expose secrets in responses or logs

| Data | Client response | Logs |
|------|-----------------|------|
| Connection strings | ❌ Never | ❌ Never (mask if needed) |
| Stack traces | ❌ Production | ✅ Server logs only |
| Internal paths | ❌ | ✅ |
| Passwords / tokens | ❌ | ❌ |
| `tracingMessage` | Dev only for 500 errors | Full detail in server logs |

**`tracingMessage` rule:**

- Success: always `null`
- `BusinessException`: `null` unless explicitly set for internal diagnostics (rare)
- Unhandled 500: `exception.ToString()` **only when `IsDevelopment()`** — production returns `null`

Implemented in `ExceptionHandlingMiddleware`.

### 4.3 Safe error messages to clients

```csharp
// ✅ Client-safe message
throw new BusinessException("Title is required.", responseCode: ResponseCodes.ValidationError);

// ❌ Leaks implementation
throw new BusinessException($"SQL error: {sqlEx.Message}");
```

Log technical details server-side; return generic `ResponseMessages.InternalError` for unexpected failures.

### 4.4 Request trace (correlation ID)

- Generated per request in `RequestTraceMiddleware`
- Client may pass `X-Request-Trace` header (must be valid GUID)
- Returned in response body **and** response header
- Use in logs: `LogError(..., "RequestTrace={RequestTrace}", requestTrace)`
- **Do not** embed user PII in trace ID

### 4.5 HTTPS and headers

- `UseHttpsRedirection()` enabled in pipeline
- Do not disable HTTPS in production configs
- When adding auth: use HTTPS-only cookies / bearer tokens; never send tokens in query strings

### 4.6 Input validation (planned)

- Validate all external input at API boundary
- Parameterized EF queries only — never string-concat SQL
- Limit page size to prevent abuse (see BR-006 in [02-business-rules.md](02-business-rules.md))

### 4.7 Dependency and package security

- Pin package versions in `.csproj` (already done for EF, Swashbuckle)
- Do not add packages from untrusted sources
- Review new NuGet dependencies before adding

---

## 5. Standard response envelope (mandatory)

All JSON API endpoints **must** use this envelope. See [05-api-contracts.md](05-api-contracts.md) for full spec.

### Success

```json
{
  "requestTrace": "25c89a97-717f-449f-a544-3ec8704767cd",
  "responseDateTime": "2026-06-12T22:22:05+07:00",
  "responseData": "OK",
  "responseStatus": {
    "responseCode": "000000",
    "responseMessage": "Success",
    "tracingMessage": null
  }
}
```

| Field | Rule |
|-------|------|
| `requestTrace` | GUID string; from middleware or `X-Request-Trace` header |
| `responseDateTime` | ISO 8601 with offset: `yyyy-MM-dd'T'HH:mm:sszzz` |
| `responseData` | Payload: object `{}`, array `[]`, or string `""` / `"OK"` |
| `responseStatus.responseCode` | `000000` for success |
| `responseStatus.responseMessage` | Default `"Success"` or contextual message |
| `responseStatus.tracingMessage` | Always `null` on success |

**Factory:** `ApiResponse<T>.Success(data, requestTrace, responseMessage?)`

### Failure

```json
{
  "requestTrace": "25c89a97-717f-449f-a544-3ec8704767cd",
  "responseDateTime": "2026-06-12T22:22:05+07:00",
  "responseData": "",
  "responseStatus": {
    "responseCode": "00000201",
    "responseMessage": "Error Message",
    "tracingMessage": null
  }
}
```

| Field | Rule |
|-------|------|
| `responseData` | **Always empty string** `""` on failure |
| `responseStatus.responseCode` | Business code from `ResponseCodes` (not HTTP status) |
| `responseStatus.responseMessage` | User-safe error description |
| `responseStatus.tracingMessage` | `null` in production; dev-only detail for unhandled 500 |

**Factory:** `ApiResponse<string>.Fail(requestTrace, responseCode, responseMessage, tracingMessage?)`

**HTTP status** still reflects REST semantics (400, 404, 500) — business code is in `responseStatus.responseCode`.

### Response code catalog (HTTP ↔ business code)

| HTTP | When | `ResponseCodes` | Value |
|------|------|-----------------|-------|
| **200 OK** | GET, PUT, PATCH success | `Success` | `000000` |
| **201 Created** | POST create success | `Success` | `000000` |
| **400 Bad Request** | Validation errors | `ValidationError` | `00000201` |
| **401 Unauthorized** | Not authenticated | `Unauthorized` | `00000204` |
| **403 Forbidden** | No permission | `Forbidden` | `00000205` |
| **404 Not Found** | Resource missing | `NotFound` | `00000202` |
| **409 Conflict** | Duplicate / conflict | `Conflict` | `00000203` |
| **429 Too Many Requests** | Rate limit exceeded | `TooManyRequests` | `00000429` |
| **500 Internal Server Error** | Unexpected error | `InternalError` | `00000500` |

Use `HttpStatusCodes` for HTTP and `ResponseCodes` for body — **always together**.

### Exception → response mapping

```csharp
throw new BusinessException(
    "Title is required.",
    statusCode: HttpStatusCodes.BadRequest,
    responseCode: ResponseCodes.ValidationError);
```

`ExceptionHandlingMiddleware` builds `ApiResponse<string>.Fail(...)` automatically.

### Excluded endpoint

`GET /health` (ASP.NET Health Checks) remains **plain text** — not this envelope. Do not change without ADR.

---

## 6. RESTful API rules

- Use **plural resource names** and correct HTTP verbs (see [05-api-contracts.md](05-api-contracts.md))
- **GET list endpoints must use pagination** — never return all rows without `page` / `pageSize`
- Defaults: `PagingDefaults.DefaultPage` (1), `DefaultPageSize` (10), max `MaxPageSize` (100)
- **POST create** → HTTP **201 Created**, not 200
- **GET / PUT / PATCH success** → HTTP **200 OK**
- Invalid paging or input → **400** + `ValidationError`
- Missing resource → **404** + `NotFound`

---

## 7. Rate limiting

- All controller endpoints use fixed-window rate limiting (`RateLimitingExtensions`)
- Configure via `appsettings.json` → `RateLimiting:PermitLimit`, `RateLimiting:WindowSeconds`
- Exceeded limit → **429** + `ResponseCodes.TooManyRequests` + standard failure envelope
- Do not disable rate limiting for public endpoints without ADR

---

## 8. Code comments (mandatory)

All production code **must include comments**. Use **simple English**.

| Situation | Comment requirement |
|-----------|---------------------|
| Public class / interface | XML `/// <summary>` describing purpose |
| Public method | XML summary + param notes when not obvious |
| Complex logic | Inline `//` explaining **why**, not what |
| Business rules | Comment referencing rule ID (e.g. `// BR-001: title required`) |
| Non-obvious workarounds | Comment with reason |

```csharp
/// <summary>
/// Returns paged todos. Large lists must always be paginated (REST rule).
/// </summary>
public async Task<PagedResult<TodoResponse>> GetPagedAsync(PagingRequest paging, ...)
{
    // Reject invalid page size before hitting the database.
    if (paging.PageSize > PagingDefaults.MaxPageSize)
        throw new BusinessException(...);

    // Skip/take calculated from 1-based page index.
    var skip = (paging.Page - 1) * paging.PageSize;
}
```

**Do not:**
- Comment every trivial line (`// increment i`)
- Write comments in Vietnamese in code (English only)
- Leave commented-out dead code in commits

---

## 9. Implementation reference

| Concern | File |
|---------|------|
| Response envelope | `Core/ViewModels/ApiResponse.cs`, `ResponseStatus.cs` |
| Response codes | `Core/Constants/ResponseCodes.cs` |
| Request trace middleware | `API/Middlewares/RequestTraceMiddleware.cs` |
| Exception → JSON | `API/Middlewares/ExceptionHandlingMiddleware.cs` |
| Get trace in controller | `API/Extensions/HttpContextExtensions.cs` |
| Rate limiting | `API/Extensions/RateLimitingExtensions.cs` |
| HTTP status constants | `Core/Constants/HttpStatusCodes.cs` |
| Paging defaults | `Core/Constants/PagingDefaults.cs` |

---

## 10. Agent checklist (every task)

- [ ] RESTful route and correct HTTP status (200/201/400/401/403/404/500)
- [ ] HTTP status matches `responseStatus.responseCode` pair
- [ ] List GET has pagination
- [ ] Rate limit config unchanged or intentionally updated
- [ ] Comments added (English) for classes and complex logic
- [ ] No hardcoded response codes or secret values
- [ ] Success/failure uses `ApiResponse<T>` factories
- [ ] `requestTrace` from `GetRequestTrace()`
- [ ] Navigation properties eager-loaded (no N+1 on list endpoints)
- [ ] `dotnet test` passes

---

## 11. N+1 Query Detection and Prevention

N+1 queries are one of the most common database performance anti-patterns in EF Core applications. Every list endpoint that returns related data must be checked against these rules before calling the feature done.

### 11.1 What is N+1?

A list endpoint returns **N resources** and fires **1 initial query + N additional queries** (one per item) — totalling **N+1 queries**. Example:

```csharp
// ❌ N+1: fetches 10 posts, then fires 10 separate queries for authors
var posts = await _repo.ListAsync<Post>();
foreach (var post in posts)
{
    post.Author = await _authorRepo.GetByIdAsync(post.AuthorId); // 10 extra queries
}
```

With 100 posts → 101 database round-trips. This causes slow response times and database connection exhaustion.

### 11.2 How to detect N+1

**Method A — EF Core logging (always on in Development):**

```json
// appsettings.Development.json — Database.Command logging set to Information
"Logging": {
  "Microsoft.EntityFrameworkCore.Database.Command": "Information"
}
```

In the server console output, look for **repeated identical query patterns** triggered within the same HTTP request:

```
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (2ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT [p].[Id], [p].[Title] FROM [Posts] AS [p]
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (3ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT [u].[Id], [u].[DisplayName] FROM [Users] AS [u] WHERE [u].[Id] = @__p_0
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (3ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      SELECT [u].[Id], [u].[DisplayName] FROM [Users] AS [u] WHERE [u].[Id] = @__p_0
```

↑ The same query repeated — classic N+1.

**Method B — Use a query profiler / EF Core diagnostics:**

In production-like testing, capture events via `IDbCommandInterceptor` or third-party tools (EF Core Profiler, MiniProfiler).

**Method C — Count queries in unit tests:**

```csharp
[Fact]
public async Task GetPosts_ShouldNotTriggerNPlusOne()
{
    // Arrange: create 5 posts with authors in the test database
    var posts = await SeedPostsWithAuthorsAsync(count: 5);

    // Act
    var result = await _controller.GetPostsAsync(paging, CancellationToken.None);

    // Assert: intercept the DbContext and count commands
    var commandCount = _dbContext.GetCommands().Count;
    Assert.True(commandCount <= 2,
        $"Expected ≤2 queries but got {commandCount}. Possible N+1 detected.");
}
```

### 11.3 How to fix N+1 — eager loading

Use **`.Include()`** or **`.ThenInclude()`** to load related data in the same query:

```csharp
// ✅ Single query using eager loading — no N+1
var posts = await _repo.ListAsync<Post>(
    include: query => query
        .Include(p => p.Author)
        .Include(p => p.Tags)
        .ThenInclude(t => t.Tag));

// EF generates ONE query with JOINs:
// SELECT p.*, a.*, t.*, tg.* FROM Posts p
// LEFT JOIN Users a ON p.AuthorId = a.Id
// LEFT JOIN PostTags pt ON p.Id = pt.PostId
// LEFT JOIN Tags tg ON pt.TagId = tg.Id
```

For paginated results, always apply `Include` **before** `Skip`/`Take`:

```csharp
// ✅ Correct order: filter → include → paginate
var posts = await _repo.ListAsync<Post>(
    where: p => p.Status == PostStatus.Published,
    include: q => q.Include(p => p.Author).Include(p => p.Tags),
    orderBy: q => q.OrderByDescending(p => p.PublishedAt),
    skip: (paging.Page - 1) * paging.PageSize,
    take: paging.PageSize);
```

### 11.4 Split queries (for large collections)

When a related collection is large (e.g. 1,000 tags per post), use **split queries** to avoid cartesian explosion in the JOIN:

```csharp
// ✅ Split query: executes 2 queries instead of 1 giant JOIN
var posts = await _repo.ListAsync<Post>(
    include: q => q
        .AsSplitQuery()
        .Include(p => p.Author)
        .Include(p => p.Tags));
```

> ⚠️ Split queries are **not** always faster. Benchmark your specific case. Use when collection size is large or when `Distinct()` is needed on the parent query.

### 11.5 Explicit loading — acceptable only after the list query

Explicit loading is **never acceptable** as the primary data-loading strategy:

```csharp
// ❌ N+1 via explicit loading
var posts = await _context.Posts.ToListAsync();
foreach (var post in posts)
    await _context.Entry(post).Collection(p => p.Tags).LoadAsync();

// ✅ Use eager loading instead
var posts = await _context.Posts.Include(p => p.Tags).ToListAsync();
```

Explicit loading is only acceptable for **conditional post-load scenarios** — e.g. loading additional detail on demand after an initial query already returned.

### 11.6 Count queries — avoid N+1 in totals

When returning `PagedResult<T>`, compute the total count in a **separate query** rather than loading all rows:

```csharp
// ✅ Two targeted queries: one for data, one for count
var totalCount = await _repo.CountAsync<Post>(where: p => p.Status == PostStatus.Published);
var posts = await _repo.ListAsync<Post>(/* same where clause */, skip, take);
```

Do **not** call `.ToListAsync()` then use `posts.Count` — that forces EF to materialize all rows just to count them.

### 11.7 Projections — when DTOs eliminate joins

Use **`.Select()` projections** to load only the fields you need, avoiding full entity materialization:

```csharp
// ✅ Projection: loads only needed columns, no full entity + no N+1
var result = await _repo.ListAsync<Post>(
    where: p => p.Status == PostStatus.Published,
    select: p => new PostSummary
    {
        Id = p.Id,
        Title = p.Title,
        AuthorName = p.Author.DisplayName,  // EF generates LEFT JOIN automatically
        TagCount = p.Tags.Count()
    },
    orderBy: q => q.OrderByDescending(p => p.PublishedAt),
    skip, take);
```

This is preferred over `.Include()` when you only need a subset of fields.

### 11.8 N+1 rules summary for agents

| Situation | Rule |
|-----------|------|
| Any list endpoint returning entities with navigation properties | **Must** use `.Include()` or `.Select()` projection |
| Multiple navigation levels | Use `.Include(x => x.Nav).ThenInclude(x => x.DeepNav)` |
| Large collections (100+ items) | Consider `.AsSplitQuery()` |
| Getting a count + data | Use two targeted queries — do not materialize all rows |
| Explicit loading inside a loop | **Forbidden** |
| `.ToListAsync()` before calling `.Include()` | **Forbidden** |
| EF log showing repeated identical queries | **Warning**: N+1 detected — fix immediately |

### 11.9 Agent checklist (N+1 per endpoint)

- [ ] List endpoint uses `.Include()` for all navigation properties
- [ ] EF log shows ≤ 2 queries (1 data + 1 count) for a paginated list
- [ ] No explicit loading or `.LoadAsync()` inside a loop
- [ ] Projection used instead of full entity when only subset of fields is needed
- [ ] `dotnet test` passes
- [ ] Response time benchmarked: list of 100 items should complete < 200 ms locally

---

## 12. Implementation reference

- [ ] RESTful route and correct HTTP status (200/201/400/401/403/404/500)
- [ ] HTTP status matches `responseStatus.responseCode` pair
- [ ] List GET has pagination
- [ ] Rate limit config unchanged or intentionally updated
- [ ] Comments added (English) for classes and complex logic
- [ ] No hardcoded response codes or secret values
- [ ] Success/failure uses `ApiResponse<T>` factories
- [ ] `requestTrace` from `GetRequestTrace()`
- [ ] `dotnet test` passes

---

## 13. Configuration and Secrets — appsettings.*.json

### 13.1 appsettings.Development.json — sensitivity rules

`appsettings.Development.json` typically contains the **local development connection string** and verbose logging levels. While it is excluded from production deployments, it **must not be committed to git** because:

- It may contain credentials or connection strings not intended for the team
- Development overrides (e.g., disabling HTTPS, lowering log levels) should be local-only
- `appsettings.Development.json` is auto-generated by the .NET scaffolding and its content varies by machine

**`appsettings.Development.json` is listed in `.gitignore` — do not remove it.**

| File | In git? | Reason |
|------|---------|--------|
| `appsettings.json` | ✅ Yes | Only safe defaults / placeholders |
| `appsettings.Development.json` | ❌ No | Local overrides, dev connection strings |
| `appsettings.*.local.json` | ❌ No | Per-developer overrides |
| `.env` | ❌ No | Real credentials (local/CI) |

### 13.2 Connection strings in Development

For local development, use `dotnet user-secrets` instead of hardcoding in `appsettings.Development.json`:

```powershell
# Set the connection string via user-secrets (never committed to git)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=...;"
```

Or use environment variables:

```powershell
# In launchSettings.json or terminal
$env:ConnectionStrings__DefaultConnection = "Server=...;Database=...;"
```

### 13.3 Adding secrets to documentation

If a secret value must appear in documentation (e.g., a sample connection string in a guide), use a clearly-marked placeholder:

```json
// ❌ Never show a real password
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TestDb;User Id=sa;Password=MySecret123"
}

// ✅ Use a clearly-marked placeholder
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

---

## Related documents

- [05-api-contracts.md](05-api-contracts.md) — full HTTP contract
- [08-ai-agent-rules.md](08-ai-agent-rules.md) — agent MUST/MUST NOT
- [09-coding-standards.md](09-coding-standards.md) — naming and patterns
- [.github/instructions/infrastructure.instructions.md](../.github/instructions/infrastructure.instructions.md) — EF Core & migrations
