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
- [ ] `dotnet test` passes

---

## Related documents

- [05-api-contracts.md](05-api-contracts.md) — full HTTP contract
- [08-ai-agent-rules.md](08-ai-agent-rules.md) — agent MUST/MUST NOT
- [09-coding-standards.md](09-coding-standards.md) — naming and patterns
- [.github/instructions/infrastructure.instructions.md](../.github/instructions/infrastructure.instructions.md) — EF Core & migrations
