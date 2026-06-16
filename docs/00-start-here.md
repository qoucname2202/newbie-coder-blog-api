# 00 — Start Here

Navigation hub for **Newbie Coder API** documentation.

---

## Read order

| Audience | Path |
|----------|------|
| AI Agent | [AGENTS.md](../AGENTS.md) → [07-security-and-clean-code.md](07-security-and-clean-code.md) → [08-ai-agent-rules.md](08-ai-agent-rules.md) |
| New developer | This file → [01-product-overview.md](01-product-overview.md) → [README.md](../README.md) |
| API work | [05-api-contracts.md](05-api-contracts.md) |
| Architecture | [04-solution-architecture.md](04-solution-architecture.md) |
| EF Core / migrations | [.github/instructions/infrastructure.instructions.md](../.github/instructions/infrastructure.instructions.md) |

---

## Documentation index

| # | File | Contents |
|---|------|----------|
| 00 | start-here | Navigation (this file) |
| 01 | product-overview | Goals, stack, roadmap |
| 02 | business-rules | Todo domain rules |
| 03 | user-flows | Client ↔ API sequences |
| 04 | solution-architecture | Clean Architecture layers |
| 05 | api-contracts | Endpoints + **standard response envelope** |
| 06 | domain-model | Entities, DTOs, interfaces |
| 07 | security-and-clean-code | **Security, anti-hardcode, response rules, code smells** |
| 08 | ai-agent-rules | Agent MUST/MUST NOT |
| 09 | coding-standards | C# conventions |
| 10 | testing | Test strategy |
| 11 | decision-log | ADRs |

> **Note:** Former `07-database.md` was removed. EF Core details are in infrastructure instructions and architecture doc.

---

## Standard response (summary)

All JSON APIs use:

- **Success:** `responseCode: "000000"`, `responseData` = payload
- **Failure:** `responseData: ""`, business code in `responseStatus.responseCode`

Full spec: [05-api-contracts.md](05-api-contracts.md) and [07-security-and-clean-code.md](07-security-and-clean-code.md).

---

## Quick commands

```powershell
.\scripts\setup.ps1
dotnet run --project NewbieCoder.API
dotnet test
```

---

## Current state

| Component | Status |
|-----------|--------|
| Standard response envelope + request trace | ✅ |
| Rate limiting on controllers | ✅ |
| REST / pagination rules (documented) | ✅ |
| TodoItem + EF migration | ✅ (no CRUD API) |
| Service layer / Auth | ❌ planned |
