# 11 — Decision Log (ADR)

Architecture Decision Records document **significant technical choices**, their context, and consequences. AI Agents should read this before proposing architectural changes and **add new ADRs** when making important decisions.

---

## ADR format template

```markdown
## ADR-XXX — Title

**Status:** Proposed | Accepted | Superseded by ADR-YYY
**Date:** YYYY-MM-DD

**Context:** What problem or constraint drove the decision?

**Decision:** What was chosen?

**Consequences:**
- (+) Benefits
- (−) Trade-offs
```

---

## ADR-001 — Clean Architecture with four projects

**Status:** Accepted  
**Date:** 2025 (initial scaffold)

**Context:** The project targets learners who need to understand layered backend structure without the complexity of a large production system.

**Decision:** Split the solution into four projects:
- `NewbieCoder.API` — presentation
- `NewbieCoder.Infrastructure` — persistence implementations
- `NewbieCoder.Core` — domain and interfaces
- `NewbieCoder.UnitTest` — tests referencing all production projects

Dependency direction: API → Infrastructure → Core.

**Consequences:**
- (+) Clear separation of concerns; Core is testable without EF or HTTP
- (+) Matches industry-standard Clean Architecture teaching material
- (−) More projects than a single-project demo; slightly higher ceremony for small features

---

## ADR-002 — Entity Framework Core with SQL Server

**Status:** Accepted

**Context:** Need a mainstream .NET ORM with migrations, strong tooling, and alignment with enterprise .NET stacks.

**Decision:** Use EF Core 8.0.11 with SQL Server provider. Store migrations in the Infrastructure project assembly.

**Consequences:**
- (+) Excellent Visual Studio and CLI tooling (`dotnet ef`)
- (+) LocalDB and Docker SQL both supported for development
- (−) Requires SQL Server (or compatible) — not zero-config like SQLite
- (−) Tests must use InMemory provider or test containers — not production SQL in unit tests

---

## ADR-003 — Generic Repository plus Unit of Work

**Status:** Accepted

**Context:** Learners need a repeatable data access pattern applicable to multiple entities without duplicating CRUD code.

**Decision:**
- Define `IRepository<T> where T : BaseEntity` in Core
- Implement `Repository<T>` in Infrastructure
- Define `IUnitOfWork` with `SaveChangesAsync` only
- Implement `EfUnitOfWork` wrapping `AppDbContext`

Repository methods stage changes; UnitOfWork commits.

**Consequences:**
- (+) Fast to add CRUD for new entities
- (+) Clear transaction boundary per request
- (−) Generic repository can become a anti-pattern for complex queries — entity-specific repositories may be added later
- (−) `IRepository<T>` is not yet registered in DI — registration needed when services consume it

---

## ADR-004 — Standard JSON response envelope

**Status:** Accepted (updated 2026-06)

**Context:** Clients need a consistent, traceable response shape for success and failure across all business endpoints.

**Decision:** All JSON controller responses use `ApiResponse<T>`:

```json
{
  "requestTrace": "uuid",
  "responseDateTime": "2026-06-12T22:22:05+07:00",
  "responseData": {},
  "responseStatus": {
    "responseCode": "000000",
    "responseMessage": "Success",
    "tracingMessage": null
  }
}
```

- Success: `responseCode` = `000000` (`ResponseCodes.Success`)
- Failure: `responseData` = `""`, business code in `responseStatus.responseCode`
- Correlation via `RequestTraceMiddleware` and `X-Request-Trace` header
- Codes centralized in `ResponseCodes` — no hardcoding

**Supersedes:** Previous `{ success, message, data }` envelope.

**Consequences:**
- (+) Traceable requests; consistent client parsing
- (+) Business codes separate from HTTP status
- (−) `/health` ASP.NET endpoint excluded (plain text)

---

## ADR-005 — Global exception middleware

**Status:** Accepted

**Context:** Controllers and services should not duplicate try/catch blocks for every action.

**Decision:** `ExceptionHandlingMiddleware` wraps the pipeline:
- `BusinessException` → HTTP status from `exception.StatusCode` + JSON `ApiResponse.Fail`
- Other exceptions → HTTP 500 + log + JSON fail

**Consequences:**
- (+) Centralized error formatting
- (+) Controllers stay thin
- (−) Developers must remember to use `BusinessException` for expected failures
- (−) Non-JSON endpoints (health check) bypass this middleware's response format naturally only if they don't throw

---

## ADR-006 — Fluent API configuration in Infrastructure

**Status:** Accepted

**Context:** Core must not depend on EF Core attributes or packages.

**Decision:** All EF mapping via `IEntityTypeConfiguration<T>` classes in `Infrastructure/Data/Configurations/`. Discover via `ApplyConfigurationsFromAssembly`.

**Consequences:**
- (+) Core entities remain persistence-ignorant
- (+) Schema details colocated in Infrastructure
- (−) Two files to maintain per entity (entity + configuration)

---

## ADR-007 — Swagger enabled only in Development

**Status:** Accepted

**Context:** Swagger is valuable for learning and local testing but increases attack surface if exposed in production without authentication.

**Decision:** Register Swagger services always, but call `UseSwagger` / `UseSwaggerUI` only when `IsDevelopment()`.

**Consequences:**
- (+) Safer default for production deployments
- (−) Production API discovery requires separate documentation or intentional Swagger enablement

---

## ADR-008 — xUnit with WebApplicationFactory

**Status:** Accepted

**Context:** Need standard .NET test tooling with integration test support for minimal API / top-level Program.

**Decision:**
- xUnit as test framework
- `Microsoft.AspNetCore.Mvc.Testing` for API tests
- `public partial class Program;` exposed for test host

**Consequences:**
- (+) Industry standard; good IDE support
- (+) Integration tests exercise real middleware pipeline
- (−) Must preserve `partial class Program` when editing entry point

---

## ADR-009 — Minimal Program.cs with extension methods

**Status:** Accepted

**Context:** Keep entry point readable; group DI and pipeline configuration.

**Decision:**
- `Program.cs` only bootstraps builder and calls `AddApiServices` / `UseApiPipeline`
- Configuration logic in `Extensions/` static classes

**Consequences:**
- (+) Clear separation; easier to navigate for learners
- (+) Testable composition
- (−) Extra indirection vs single-file Program

---

## ADR-010 — Application service layer

**Status:** Proposed  
**Date:** 2026

**Context:** Todo CRUD requires business validation, entity-to-DTO mapping, and orchestration. Controllers should not call repositories directly for non-trivial features.

**Decision (proposed):**
- Add `ITodoService` in `Core/Interfaces/Services/`
- Implement `TodoService` in `Infrastructure/Services/`
- Controllers depend on `ITodoService` only
- Register as Scoped in DI

**Alternatives considered:**
- Separate `NewbieCoder.Application` project — rejected for now due to learning-project scope
- Controllers call `IRepository<TodoItem>` directly — rejected; violates thin controller goal

**Consequences (if accepted):**
- (+) Business rules centralized and unit-testable
- (+) Clear place for validation before persistence
- (−) Additional abstraction layer for simple CRUD

**Action:** Mark Accepted and update ADR date when Todo CRUD is implemented.

---

## ADR-011 — JWT Bearer authentication

**Status:** Proposed

**Context:** Production APIs require authentication. Listed in README roadmap.

**Decision (proposed):** Implement JWT Bearer authentication using ASP.NET Core Identity or lightweight JWT middleware; secure Todo endpoints per user.

**Open questions:**
- User entity and storage
- Todo ownership model (`UserId` on TodoItem?)
- Refresh tokens vs short-lived access tokens

**Consequences (if accepted):**
- (+) Industry-standard auth pattern
- (−) Significant schema and API contract changes
- (−) Requires updates to all docs and test auth helpers

---

## ADR-012 — FluentValidation for request DTOs

**Status:** Proposed

**Context:** Request validation should not be scattered in controllers or services without structure.

**Decision (proposed):** Add FluentValidation; validators colocated with DTOs or in dedicated folder; pipeline integration in API.

**Consequences (if accepted):**
- (+) Declarative, testable validation rules
- (−) New NuGet dependency; learning curve for beginners

---

## ADR-013 — English as documentation language

**Status:** Accepted  
**Date:** 2026-06

**Context:** AI Agents and international contributors benefit from a single canonical language for architecture and rules documentation.

**Decision:** Maintain `AGENTS.md`, `docs/`, and `.github/instructions/` in **English**. Human README may remain bilingual or Vietnamese — agents should prefer English docs for architecture truth.

**Consequences:**
- (+) Consistent agent context regardless of user locale
- (+) Aligns with code identifiers (English)
- (−) Vietnamese-speaking learners may prefer localized docs — README still available

---

## Superseded decisions

None yet.

---

## How agents should use this log

| Situation | Action |
|-----------|--------|
| Implementing proposed ADR (010, 011, 012) | Change status to Accepted; add date; implement per decision |
| Choosing between architectural alternatives | Add new Proposed ADR; ask user if significant |
| Reversing a decision | Mark old ADR Superseded; reference new ADR number |
| Minor implementation detail | Do not create ADR — update relevant docs/ only |

---

## Related documents

- [04-solution-architecture.md](04-solution-architecture.md) — current architecture
- [08-ai-agent-rules.md](08-ai-agent-rules.md) — agent constraints
- [01-product-overview.md](01-product-overview.md) — roadmap
