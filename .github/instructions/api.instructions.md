---
applyTo: "NewbieCoder.API/**"
---

# API Layer — NewbieCoder.API

## RESTful rules

- Plural resources: `/api/todos`
- **GET list → mandatory pagination** (`page`, `pageSize`, max `PagingDefaults.MaxPageSize`)
- POST create → **201 Created** via `StatusCode(HttpStatusCodes.Created, ...)`
- GET / PUT / PATCH → **200 OK**

## HTTP status ↔ responseCode

| HTTP | responseCode |
|------|--------------|
| 200 / 201 | `000000` |
| 400 | `00000201` |
| 401 | `00000204` |
| 403 | `00000205` |
| 404 | `00000202` |
| 429 | `00000429` |
| 500 | `00000500` |

Use `HttpStatusCodes` + `ResponseCodes` + `BusinessException`.

## Rate limiting

- Applied: `MapControllers().RequireRateLimiting("default")`
- Config: `RateLimiting:PermitLimit`, `RateLimiting:WindowSeconds`
- 429 → standard failure envelope

## Comments

- XML `/// summary` on controllers and actions
- English inline comments for complex logic

## Controller pattern

```csharp
var trace = httpContextAccessor.HttpContext!.GetRequestTrace();
return Ok(ApiResponse<T>.Success(data, trace));

return StatusCode(HttpStatusCodes.Created, ApiResponse<T>.Success(created, trace));
```

See [docs/05-api-contracts.md](../../docs/05-api-contracts.md).
