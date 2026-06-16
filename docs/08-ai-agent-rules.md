# 08 — AI Agent Rules

Mandatory rules for AI Agents. Read with [AGENTS.md](../AGENTS.md) and [07-security-and-clean-code.md](07-security-and-clean-code.md).

---

## MUST

### Architecture
| # | Rule |
|---|------|
| A1 | Dependency direction: API → Infrastructure → Core |
| A2 | Core has zero EF/ASP.NET/Infrastructure references |
| A3 | Interfaces in Core; implementations in Infrastructure |
| A4 | EF Fluent config in `Infrastructure/Data/Configurations/` |
| A5 | Migrations via `dotnet ef` in Infrastructure project |

### RESTful API
| # | Rule |
|---|------|
| REST1 | Plural resource URLs; correct HTTP verbs |
| REST2 | **GET list → mandatory pagination** (`page`, `pageSize`, max 100) |
| REST3 | POST create → **201 Created**; GET/PUT/PATCH → **200 OK** |
| REST4 | Never return unbounded record sets |

### HTTP status ↔ responseCode (exact mapping)
| # | Rule |
|---|------|
| H1 | **200** GET/PUT/PATCH success → `ResponseCodes.Success` (`000000`) |
| H2 | **201** POST create → `ResponseCodes.Success` (`000000`) |
| H3 | **400** validation → `ResponseCodes.ValidationError` (`00000201`) |
| H4 | **401** not authenticated → `ResponseCodes.Unauthorized` (`00000204`) |
| H5 | **403** no permission → `ResponseCodes.Forbidden` (`00000205`) |
| H6 | **404** not found → `ResponseCodes.NotFound` (`00000202`) |
| H7 | **500** unexpected → `ResponseCodes.InternalError` (`00000500`) |
| H8 | Use `HttpStatusCodes` + `ResponseCodes` — never wrong pairings |

### Rate limiting
| # | Rule |
|---|------|
| RL1 | Do not bypass controller rate limiting without ADR |
| RL2 | Rate limit exceeded → **429** + `ResponseCodes.TooManyRequests` |

### Code comments
| # | Rule |
|---|------|
| CM1 | Add comments to all new classes and non-trivial methods |
| CM2 | Complex logic → inline comments in **simple English** |
| CM3 | XML `/// summary` on public APIs |

### Response envelope
| # | Rule |
|---|------|
| R1 | `ApiResponse<T>.Success(data, requestTrace)` |
| R2 | Fail via `BusinessException` + middleware |
| R3 | Failure `responseData` is always `""` |
| R4 | Never hardcode response code strings |

### Quality & security
| # | Rule |
|---|------|
| S1 | No secrets in code, logs, or responses |
| S6 | Run `dotnet test` after changes |
| S7 | Keep `public partial class Program;` |

---

## MUST NOT

| # | Forbidden |
|---|-----------|
| F1 | Wrong HTTP status for scenario (e.g. 200 on validation fail) |
| F2 | Mismatched HTTP status and `responseCode` |
| F3 | List GET without pagination |
| F4 | Hardcoded status codes or response codes |
| F5 | Disable rate limiting without approval |
| F6 | Complex code without English comments |
| F7 | Inject `AppDbContext` into controllers |
| F8 | Commit/push without user request |

---

## Reference

```csharp
throw new BusinessException(
    "Title is required.",
    statusCode: HttpStatusCodes.BadRequest,
    responseCode: ResponseCodes.ValidationError);

return StatusCode(HttpStatusCodes.Created, ApiResponse<T>.Success(dto, trace));
```

See [05-api-contracts.md](05-api-contracts.md) for full REST and status tables.

---

## Completion checklist

- [ ] RESTful design + pagination for lists
- [ ] HTTP status ↔ responseCode correct
- [ ] English comments on new/complex code
- [ ] Rate limiting respected
- [ ] `dotnet test` passes
