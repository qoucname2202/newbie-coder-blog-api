# 05 — API Contracts

Authoritative **public HTTP contract** for Newbie Coder API.

---

## Base URLs

| Environment | Base URL |
|-------------|----------|
| Local HTTPS | `https://localhost:7020` |
| Local HTTP | `http://localhost:5029` |
| Docker | `http://localhost:5089` |

---

## RESTful API standards (mandatory)

All new endpoints **must** follow REST conventions.

### Resource naming

| Rule | Example |
|------|---------|
| Use **plural nouns** for collections | `/api/todos`, not `/api/todo` |
| Use **GUID** for single resource | `GET /api/todos/{id:guid}` |
| No verbs in URL | `POST /api/todos`, not `/api/createTodo` |
| Nested resources only when ownership is clear | `/api/todos/{id}/comments` (future) |

### HTTP methods and status codes

| Method | Purpose | Success HTTP | Notes |
|--------|---------|--------------|-------|
| **GET** | Read one or many | **200 OK** | Collection GET **must** support paging |
| **POST** | Create resource | **201 Created** | Return created resource in `responseData` |
| **PUT** | Full replace | **200 OK** | |
| **PATCH** | Partial update | **200 OK** | |
| **DELETE** | Remove resource | **200 OK** or **204 No Content** | Pick one project-wide |

### Pagination (mandatory for large collections)

Any **GET** that returns a **list** of records **must** use paging — never return unbounded lists.

**Query parameters:**

| Param | Type | Default | Rules |
|-------|------|---------|-------|
| `page` | int | `1` (`PagingDefaults.DefaultPage`) | Must be >= 1 |
| `pageSize` | int | `10` (`PagingDefaults.DefaultPageSize`) | 1–100 (`PagingDefaults.MaxPageSize`) |

Invalid paging → **400 Bad Request** + `ResponseCodes.ValidationError`.

**Success `responseData` shape (planned):**

```json
{
  "items": [],
  "page": 1,
  "pageSize": 10,
  "totalCount": 0,
  "totalPages": 0,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

**Controller example (planned):**

```csharp
[HttpGet]
public async Task<ActionResult<ApiResponse<PagedResult<TodoResponse>>>> GetList(
    [FromQuery] PagingRequest paging,
    CancellationToken cancellationToken)
{
    var trace = httpContextAccessor.HttpContext!.GetRequestTrace();
    var result = await todoService.GetPagedAsync(paging, cancellationToken);
    return Ok(ApiResponse<PagedResult<TodoResponse>>.Success(result, trace));
}
```

### POST create — 201 Created

```csharp
var trace = httpContextAccessor.HttpContext!.GetRequestTrace();
var created = await todoService.CreateAsync(request, cancellationToken);
return StatusCode(HttpStatusCodes.Created, ApiResponse<TodoResponse>.Success(created, trace));
```

---

## HTTP status codes and business responseCode (mandatory mapping)

**Both** the HTTP status line **and** `responseStatus.responseCode` in the JSON body must be correct and consistent.

Use constants from `HttpStatusCodes` and `ResponseCodes` — never magic numbers or inline strings.

| HTTP status | When to use | `ResponseCodes` constant | `responseCode` value |
|-------------|-------------|--------------------------|----------------------|
| **200 OK** | Successful GET, PUT, PATCH | `Success` | `000000` |
| **201 Created** | Successful POST create | `Success` | `000000` |
| **400 Bad Request** | Validation errors, invalid input | `ValidationError` | `00000201` |
| **401 Unauthorized** | User not authenticated | `Unauthorized` | `00000204` |
| **403 Forbidden** | User has no permission | `Forbidden` | `00000205` |
| **404 Not Found** | Resource does not exist | `NotFound` | `00000202` |
| **409 Conflict** | Duplicate / conflict | `Conflict` | `00000203` |
| **429 Too Many Requests** | Rate limit exceeded | `TooManyRequests` | `00000429` |
| **500 Internal Server Error** | Unexpected server error | `InternalError` | `00000500` |

### Throwing errors with correct pairing

```csharp
// 400 — validation
throw new BusinessException(
    "Title is required.",
    statusCode: HttpStatusCodes.BadRequest,
    responseCode: ResponseCodes.ValidationError);

// 404 — not found
throw new BusinessException(
    "Todo item not found.",
    statusCode: HttpStatusCodes.NotFound,
    responseCode: ResponseCodes.NotFound);

// 401 — not authenticated (when auth is added)
throw new BusinessException(
    "Authentication required.",
    statusCode: HttpStatusCodes.Unauthorized,
    responseCode: ResponseCodes.Unauthorized);

// 403 — no permission
throw new BusinessException(
    "You do not have permission to access this resource.",
    statusCode: HttpStatusCodes.Forbidden,
    responseCode: ResponseCodes.Forbidden);

// 500 — only for unhandled exceptions (middleware); do not throw manually for expected cases
```

**Rule:** `BusinessException.StatusCode` must match the HTTP row above; `BusinessException.ResponseCode` must match the business code column. If `responseCode` is omitted, `ResponseCodes.FromHttpStatus(statusCode)` is used.

---

## Rate limiting

All **controller** endpoints are protected by a **fixed-window rate limiter**.

| Setting | Config key | Default |
|---------|------------|---------|
| Max requests per window | `RateLimiting:PermitLimit` | 100 |
| Window length (seconds) | `RateLimiting:WindowSeconds` | 60 |

When exceeded → **HTTP 429** + standard failure envelope:

```json
{
  "requestTrace": "...",
  "responseDateTime": "...",
  "responseData": "",
  "responseStatus": {
    "responseCode": "00000429",
    "responseMessage": "Too many requests. Please try again later.",
    "tracingMessage": null
  }
}
```

**Excluded:** `GET /health` (ASP.NET Health Checks) — not mapped through controller rate limit policy.

Implementation: `RateLimitingExtensions.cs`, applied via `MapControllers().RequireRateLimiting(...)`.

---

## Global JSON response envelope

### Success

```json
{
  "requestTrace": "25c89a97-717f-449f-a544-3ec8704767cd",
  "responseDateTime": "2026-06-12T22:22:05+07:00",
  "responseData": {},
  "responseStatus": {
    "responseCode": "000000",
    "responseMessage": "Success",
    "tracingMessage": null
  }
}
```

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

Factories: `ApiResponse<T>.Success(...)`, `ApiResponse<string>.Fail(...)`.

Header: `X-Request-Trace` — set by `RequestTraceMiddleware`.

---

## Implemented endpoints

### GET /api/health

| Property | Value |
|----------|-------|
| HTTP success | **200 OK** |
| `responseCode` | `000000` |
| Rate limited | Yes |

---

### GET /health

Plain text ASP.NET Health Checks — **not** the standard JSON envelope. HTTP 200 / 503.

---

## Agent maintenance

When adding/changing endpoints:

1. Follow REST method + HTTP status table above
2. List GET → mandatory paging
3. Update this file
4. Use `HttpStatusCodes` + `ResponseCodes` pairs
5. Add comments on non-obvious logic (English)
6. Add `ApiTests/`

See also [07-security-and-clean-code.md](07-security-and-clean-code.md).
