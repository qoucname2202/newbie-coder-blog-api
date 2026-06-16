---
applyTo: "**/*"
---

# Backend — General Rules

## RESTful API

- Correct HTTP verbs and status codes (200, 201, 400, 401, 403, 404, 500)
- List GET **must** paginate — see `PagingDefaults`
- Full spec: [docs/05-api-contracts.md](../../docs/05-api-contracts.md)

## HTTP ↔ responseCode

Always pair `HttpStatusCodes` with `ResponseCodes`. Never mismatch (e.g. 404 with `00000201`).

## Rate limiting

Required on controllers. Do not remove without ADR.

## Code comments

Mandatory in simple English — XML on public members, inline on complex logic.

## Security

No secrets in code or responses. See [docs/07-security-and-clean-code.md](../../docs/07-security-and-clean-code.md).

## Architecture

```text
API → Infrastructure → Core
```

Standard JSON envelope via `ApiResponse<T>`.
