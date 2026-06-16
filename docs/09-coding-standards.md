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

## Agent checklist

- [ ] REST + correct HTTP/error code pairing
- [ ] Pagination on list endpoints
- [ ] English comments on new classes/complex logic
- [ ] Standard response envelope
- [ ] `dotnet test` passes
