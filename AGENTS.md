# AGENTS.md — AI Agent Entry Point

Primary entry for AI Agents working in **Newbie Coder API**.

---

## Critical rules (summary)

### RESTful API
- Plural resources (`/api/todos`), correct HTTP verbs
- **GET list → mandatory pagination** (`page`, `pageSize`, max 100)
- POST create → **201**; GET/PUT/PATCH success → **200**

### HTTP status ↔ responseCode (must match)

| HTTP | When | responseCode |
|------|------|--------------|
| 200 | GET, PUT, PATCH success | `000000` |
| 201 | POST create success | `000000` |
| 400 | Validation errors | `00000201` |
| 401 | Not authenticated | `00000204` |
| 403 | No permission | `00000205` |
| 404 | Not found | `00000202` |
| 429 | Rate limit exceeded | `00000429` |
| 500 | Unexpected error | `00000500` |

Use `HttpStatusCodes` + `ResponseCodes` constants.

### Rate limiting
- Enabled on all controllers (default: 100 req / 60s)
- Config: `RateLimiting` in `appsettings.json`

### Code comments
- **Mandatory** — XML summary on classes/methods; inline English comments on complex logic

### Response envelope

**Success:** `requestTrace`, `responseDateTime`, `responseData`, `responseStatus.responseCode: "000000"`

**Failure:** `responseData: ""`, matching business code in `responseStatus.responseCode`

---

## Documentation map

| Task | Read |
|------|------|
| API / REST / HTTP codes | [docs/05-api-contracts.md](docs/05-api-contracts.md) |
| Security, clean code, comments | [docs/07-security-and-clean-code.md](docs/07-security-and-clean-code.md) |
| Agent rules | [docs/08-ai-agent-rules.md](docs/08-ai-agent-rules.md) |
| EF Core | `.github/instructions/infrastructure.instructions.md` |

---

## Commands

```powershell
dotnet run --project NewbieCoder.API
dotnet test
```

---

## Feature snapshot

| Feature | Status |
|---------|--------|
| Standard response envelope | ✅ |
| Rate limiting | ✅ |
| Request trace middleware | ✅ |
| REST / pagination rules (docs) | ✅ |
| Todo CRUD API | ❌ planned |
| Authentication | ❌ planned |
