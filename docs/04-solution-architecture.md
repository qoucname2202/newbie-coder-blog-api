# 04 — Solution Architecture

This document is the **authoritative architecture reference** for Newbie Coder API. AI Agents must read this before adding features, new projects, or cross-layer refactors.

---

## Architectural style

**Clean Architecture** (also called Onion Architecture or Ports and Adapters simplified):

- **Independence of frameworks** — Core has no EF or ASP.NET references
- **Testability** — business rules testable without database or web server
- **Independence of UI** — API is one delivery mechanism; Core is reusable
- **Independence of database** — Core defines interfaces; Infrastructure implements SQL Server

---

## Solution diagram

```text
┌──────────────────────────────────────────────────────────────┐
│                    NewbieCoder.API                           │
│  Presentation / HTTP Adapter                                 │
│  • Controllers          • Middleware                         │
│  • Swagger / Health     • DI composition (AddApiServices)    │
└────────────────────────────┬─────────────────────────────────┘
                             │ project reference
┌────────────────────────────▼─────────────────────────────────┐
│               NewbieCoder.Infrastructure                     │
│  Infrastructure / Data Adapter                               │
│  • AppDbContext         • Repository<T>                      │
│  • EF Configurations    • EfUnitOfWork                       │
│  • Migrations           • AddInfrastructure()                  │
└────────────────────────────┬─────────────────────────────────┘
                             │ project reference
┌────────────────────────────▼─────────────────────────────────┐
│                   NewbieCoder.Core                           │
│  Domain + Application Contracts                              │
│  • Entities             • IRepository<T>, IUnitOfWork        │
│  • DTOs, ViewModels     • BusinessException                  │
│  • Constants, Enums     • (future) IService interfaces       │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│                 NewbieCoder.UnitTest                         │
│  references: API + Infrastructure + Core                      │
└──────────────────────────────────────────────────────────────┘
```

---

## The Dependency Rule

> Source code dependencies must point **inward** toward the Core.

| From | May reference | Must NOT reference |
|------|---------------|-------------------|
| Core | BCL only | Infrastructure, API, EF, ASP.NET |
| Infrastructure | Core | API |
| API | Infrastructure (transitively Core) | — (but must not bypass layers improperly) |
| UnitTest | All production projects | — |

**Violations to reject:**
- `using Microsoft.EntityFrameworkCore` in Core
- `AppDbContext` injected into a controller
- Business logic duplicated in controller and repository

---

## Layer responsibilities (detailed)

### NewbieCoder.API — Presentation

| Component | File path | Responsibility |
|-----------|-----------|----------------|
| Entry | `Program.cs` | Bootstrap; call `AddApiServices` + `UseApiPipeline` |
| DI registration | `Extensions/ServiceCollectionExtensions.cs` | Register infrastructure, health checks, MVC, Swagger |
| Pipeline | `Extensions/ApplicationBuilderExtensions.cs` | Middleware ordering |
| Exception handling | `Middlewares/ExceptionHandlingMiddleware.cs` | Catch all exceptions → JSON |
| Controllers | `Controllers/HealthController.cs` | HTTP request handlers |
| Config | `appsettings*.json` | Connection strings, logging levels |

**Controller responsibility boundary:**
- Parse route/query/body into DTOs
- Call application service (future) or orchestrate minimal logic
- Map result to `ActionResult<ApiResponse<T>>`
- **Must not:** query EF directly, implement business rules, call `SaveChangesAsync` without a clear pattern

### NewbieCoder.Core — Domain and contracts

| Component | File path | Responsibility |
|-----------|-----------|----------------|
| Entities | `Entities/BaseEntity.cs`, `TodoItem.cs` | Domain state |
| ViewModels | `ViewModels/ApiResponse.cs` | HTTP response envelope |
| DTOs | `DTOs/PagingRequest.cs` | Input/output contracts |
| Interfaces | `Interfaces/Repositories/*.cs` | Persistence abstractions |
| Exceptions | `Exceptions/BusinessException.cs` | Controlled failure type |
| Constants | `Constants/AppConstants.cs` | Shared constants (API metadata) |
| Enums | `Enums/SortDirection.cs` | Shared enumerations |

Core contains **no implementation** of database or HTTP — only types and contracts.

### NewbieCoder.Infrastructure — Technical implementation

| Component | File path | Responsibility |
|-----------|-----------|----------------|
| DbContext | `Data/AppDbContext.cs` | EF Core session, DbSets |
| Configurations | `Data/Configurations/TodoItemConfiguration.cs` | Table/column mapping |
| Migrations | `Data/Migrations/*` | Schema versioning |
| Repository | `Repositories/Repository.cs` | Generic CRUD implementation |
| Unit of Work | `UnitOfWork/UnitOfWork.cs` | `SaveChangesAsync` wrapper |
| DI | `DependencyInjection.cs` | Wire DbContext + UoW |

Infrastructure **implements** Core interfaces. It may contain query logic but should not contain HTTP-specific code.

### NewbieCoder.UnitTest — Verification

| Folder | Tests |
|--------|-------|
| `CoreTests/` | Pure unit tests (ApiResponse, future domain logic) |
| `RepositoryTests/` | DbContext + UnitOfWork with InMemory provider |
| `ServiceTests/` | Business services (placeholder today) |
| `ApiTests/` | Full HTTP pipeline via WebApplicationFactory |

---

## Request lifecycle (implemented)

```text
                    HTTP Request
                         │
                         ▼
              ┌──────────────────────┐
              │ ExceptionHandling    │  ← outermost: catches all downstream errors
              │ Middleware           │
              └──────────┬───────────┘
                         │
         ┌───────────────┼───────────────┐
         ▼               ▼               ▼
    Swagger (dev)   HTTPS redirect   Authorization (no auth yet)
                         │
                         ▼
                  ┌─────────────┐
                  │ Controller  │  ← HealthController today
                  └──────┬──────┘
                         │
              (future: Service layer)
                         │
                         ▼
              ┌─────────────────────┐
              │ IRepository<T>      │
              │ IUnitOfWork         │
              └──────────┬──────────┘
                         │
                         ▼
              ┌─────────────────────┐
              │ AppDbContext        │
              │ (EF Core)           │
              └──────────┬──────────┘
                         │
                         ▼
                   SQL Server
```

---

## Composition root and DI

ASP.NET Core DI container is the **composition root**. Registration happens in:

1. `ServiceCollectionExtensions.AddApiServices()` — API-level services
2. `DependencyInjection.AddInfrastructure()` — persistence services

### Current registrations

| Service | Interface | Implementation | Lifetime |
|---------|-----------|----------------|----------|
| DbContext | — | `AppDbContext` | Scoped |
| Unit of Work | `IUnitOfWork` | `EfUnitOfWork` | Scoped |
| Health check | — | SQL Server probe | Singleton config |

Generic `IRepository<T>` is **not** registered in DI today — services would inject it after registration or use a dedicated repository.

### Scoped lifetime rationale

One HTTP request = one DbContext scope = one UnitOfWork. All repositories in the same request share the same context instance.

---

## Planned: Application / Service layer

Today controllers would talk directly to repositories — **avoid this for Todo CRUD**. Introduce:

```text
Core/Interfaces/Services/ITodoService.cs     ← contract
Infrastructure/Services/TodoService.cs       ← implementation (or separate Application project if growth warrants)
```

Service layer responsibilities:
- Enforce business rules (see [02-business-rules.md](02-business-rules.md))
- Map entities ↔ DTOs
- Coordinate repository + unit of work
- Throw `BusinessException` for predictable failures

Controllers depend on `ITodoService`, not `IRepository<TodoItem>`.

---

## Cross-cutting concerns

| Concern | Implementation | Location |
|---------|----------------|----------|
| **Logging** | ASP.NET Core default | `appsettings.json` LogLevel |
| **Exception handling** | Middleware | `ExceptionHandlingMiddleware.cs` |
| **Health monitoring** | `/health` SQL probe + `/api/health` | API extensions + HealthController |
| **API documentation** | Swagger | Development only |
| **Validation** | Not implemented | Future: FluentValidation in API pipeline |
| **Authentication** | Not implemented | Future: JWT bearer |
| **CORS** | Not configured | Add in API if SPA client added |

---

## Data flow patterns

### Read (future Todo GET)

```text
Controller → Service → Repository.GetByIdAsync (NoTracking) → return DTO
```

### Write (future Todo POST)

```text
Controller → Service → validate → build entity → Repository.AddAsync → UnitOfWork.SaveChangesAsync → return DTO
```

**Single SaveChanges per request** for a given unit of work unless explicit transaction scope is added.

---

## Deployment topology

### Local development (Windows)

```text
Browser / Swagger
       │
       ▼
NewbieCoder.API  :5029 / :7020
       │
       ▼
SQL Server LocalDB  (localdb)\mssqllocaldb
Database: NewbieCoderDb
```

### Docker Compose

```text
Host browser → localhost:5089 → api container :8080
                                    │
                                    ▼
                              sqlserver container :1433
                              volume: sqlserver_data
```

Files: `docker-compose.yml`, `Dockerfile`

---

## CI architecture

GitHub Actions (`.github/workflows/ci.yml`):

```text
push/PR to main
    │
    ├── job: restore (dotnet tool restore + dotnet restore)
    ├── job: build (Release, depends on restore)
    └── job: test (Release, depends on build)
```

No database service in CI — tests use InMemory and WebApplicationFactory.

---

## Extension guidelines for agents

When adding a vertical slice (e.g., Todo CRUD):

1. Start from Core (DTOs, service interface, business rules)
2. Implement Infrastructure (service impl, DI registration if needed)
3. Add API controller (thin)
4. Add migration if entity changed
5. Add tests at each layer
6. Update docs 05, 06, 07 and ADR if architectural choice made

**Do not:**
- Skip layers and put EF queries in controllers
- Add circular project references
- Put Swagger or middleware in Infrastructure

---

## Related documents

- [06-domain-model.md](06-domain-model.md) — types and interfaces
- [07-security-and-clean-code.md](07-security-and-clean-code.md) — security, clean code, response rules
- [08-ai-agent-rules.md](08-ai-agent-rules.md) — agent constraints
- [11-decision-log.md](11-decision-log.md) — ADRs explaining why
