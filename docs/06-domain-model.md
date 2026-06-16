# 06 — Domain Model

Domain types and contracts in `NewbieCoder.Core`.

---

## Response envelope types

### ApiResponse&lt;T&gt;

**File:** `ViewModels/ApiResponse.cs`

| Property | JSON name | Description |
|----------|-----------|-------------|
| `RequestTrace` | requestTrace | Correlation GUID |
| `ResponseDateTime` | responseDateTime | ISO 8601 with offset |
| `ResponseData` | responseData | Success payload (T) |
| `ResponseStatus` | responseStatus | Code + message + tracing |

**Factories:**

```csharp
ApiResponse<T>.Success(T data, string requestTrace, string responseMessage = "Success", ...)
ApiResponse<string>.Fail(string requestTrace, string responseCode, string responseMessage, ...)
```

### ResponseStatus

**File:** `ViewModels/ResponseStatus.cs`

| Property | Description |
|----------|-------------|
| `ResponseCode` | Business code e.g. `000000`, `00000201` |
| `ResponseMessage` | Human-readable message |
| `TracingMessage` | Diagnostic detail — null in production for 500 |

---

## Constants

### ResponseCodes

**File:** `Constants/ResponseCodes.cs`

| Constant | Value |
|----------|-------|
| `Success` | `000000` |
| `ValidationError` | `00000201` |
| `NotFound` | `00000202` |
| `Conflict` | `00000203` |
| `Unauthorized` | `00000204` |
| `Forbidden` | `00000205` |
| `TooManyRequests` | `00000429` |
| `InternalError` | `00000500` |

### HttpStatusCodes

**File:** `Constants/HttpStatusCodes.cs` — `Ok` (200), `Created` (201), `BadRequest` (400), `Unauthorized` (401), `Forbidden` (403), `NotFound` (404), `TooManyRequests` (429), `InternalServerError` (500).

### PagingDefaults

**File:** `Constants/PagingDefaults.cs` — `DefaultPage` (1), `DefaultPageSize` (10), `MaxPageSize` (100).

### ResponseMessages

**File:** `Constants/ResponseMessages.cs`

- `Success` = `"Success"`
- `InternalError` = `"An unexpected error occurred."`
- `TooManyRequests` = `"Too many requests. Please try again later."`

### HttpContextItemKeys

**File:** `Constants/HttpContextItemKeys.cs`

- `RequestTrace` — key for middleware-stored correlation ID

---

## Entities

### BaseEntity

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### TodoItem

```csharp
public class TodoItem : BaseEntity
{
    public required string Title { get; set; }
    public bool IsCompleted { get; set; }
}
```

EF mapping: `Infrastructure/Data/Configurations/TodoItemConfiguration.cs`

---

## Exceptions

### BusinessException

```csharp
public BusinessException(
    string message,
    int statusCode = 400,
    string? responseCode = null,
    string? tracingMessage = null)
```

| Property | Purpose |
|----------|---------|
| `StatusCode` | HTTP status for middleware |
| `ResponseCode` | Business code in response body |
| `TracingMessage` | Optional diagnostic (usually null) |

---

## Repository contracts

- `IRepository<T> where T : BaseEntity` — CRUD staging
- `IUnitOfWork` — `SaveChangesAsync` boundary

See [04-solution-architecture.md](04-solution-architecture.md) for layer usage.

---

## Deprecated shape (do not use)

Old envelope `{ success, message, data }` is **removed**. Always use the standard envelope documented in [05-api-contracts.md](05-api-contracts.md).

---

## EF Core / database

Schema and migration workflow: [.github/instructions/infrastructure.instructions.md](../.github/instructions/infrastructure.instructions.md)

Table `TodoItems` — created by migration `InitialCreate`.
