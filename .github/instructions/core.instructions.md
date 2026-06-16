---
applyTo: "NewbieCoder.Core/**"
---

# Core Layer — NewbieCoder.Core

## Hard rules

- No references to Infrastructure, API, EF, or ASP.NET
- No NuGet packages beyond BCL
- No EF attributes on entities

## Response envelope types

### ApiResponse&lt;T&gt; (`ViewModels/ApiResponse.cs`)

| Property | Purpose |
|----------|---------|
| RequestTrace | Correlation GUID |
| ResponseDateTime | ISO 8601 with offset |
| ResponseData | Payload (success) or `""` (failure via Fail factory) |
| ResponseStatus | Code, message, tracingMessage |

```csharp
ApiResponse<T>.Success(T data, string requestTrace, string responseMessage = ResponseMessages.Success)
ApiResponse<string>.Fail(string requestTrace, string responseCode, string responseMessage, string? tracingMessage = null)
```

### ResponseStatus (`ViewModels/ResponseStatus.cs`)

- `ResponseCode` — use `ResponseCodes` constants only
- `ResponseMessage` — client-safe text
- `TracingMessage` — null unless explicit diagnostic

## Constants (no hardcoding)

| Class | Purpose |
|-------|---------|
| `ResponseCodes` | `000000`, `00000201`, etc. |
| `ResponseMessages` | Default success/internal error text |
| `HttpContextItemKeys` | `RequestTrace` key name |
| `AppConstants` | Swagger title/version |

**Never** write `"000000"` or `"00000201"` as literals in Core or any layer.

## BusinessException

```csharp
throw new BusinessException(
    "Title is required.",
    statusCode: 400,
    responseCode: ResponseCodes.ValidationError);
```

Properties: `StatusCode` (HTTP), `ResponseCode` (body), optional `TracingMessage`.

## Entities & repositories

- Entities inherit `BaseEntity` — no EF attributes
- `IRepository<T>`, `IUnitOfWork` — persistence contracts

See [docs/06-domain-model.md](../../docs/06-domain-model.md).
