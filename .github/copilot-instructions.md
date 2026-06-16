# GitHub Copilot — Newbie Coder API

Clean Architecture ASP.NET Core Web API (.NET 8) with EF Core.

## Architecture

```text
API → Infrastructure → Core
```

## Standard JSON response

**Success:** `requestTrace`, `responseDateTime`, `responseData` (payload), `responseStatus.responseCode: "000000"`

**Failure:** `responseData: ""`, `responseStatus.responseCode: "00000201"` (or other `ResponseCodes`)

Use `ApiResponse<T>.Success(data, requestTrace)` and `BusinessException` + middleware for errors.

## Clean code & security

- No hardcoded response codes — use `ResponseCodes`
- No secrets in code, git, logs, or API responses
- `requestTrace` from `HttpContext.GetRequestTrace()`
- See [docs/07-security-and-clean-code.md](docs/07-security-and-clean-code.md)

## Key files

| File | Role |
|------|------|
| `Core/ViewModels/ApiResponse.cs` | Response envelope |
| `Core/Constants/ResponseCodes.cs` | Business codes |
| `API/Middlewares/RequestTraceMiddleware.cs` | Correlation ID |
| `API/Middlewares/ExceptionHandlingMiddleware.cs` | Error envelope |

## Layer instructions

`.github/instructions/` — api, core, infrastructure, testing, backend

## Entry point

[AGENTS.md](AGENTS.md)
