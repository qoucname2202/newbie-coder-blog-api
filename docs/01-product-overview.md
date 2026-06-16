# 01 — Product Overview

## Product name

**Newbie Coder API**

## Mission

Provide a **production-shaped but education-focused** ASP.NET Core Web API that teaches:

- Clean Architecture and dependency direction
- Entity Framework Core with SQL Server
- Repository and Unit of Work patterns
- Global exception handling and consistent API responses
- Unit and integration testing
- CI/CD and containerized local development

The codebase is intentionally small so learners (and AI Agents) can understand the full stack without noise.

---

## Target audience

| Audience | How they use the project |
|----------|--------------------------|
| Beginner .NET developers | Learn layered architecture by reading and extending code |
| Bootcamp / self-study students | Follow README setup, then implement Todo CRUD |
| AI coding agents | Use `AGENTS.md` and `docs/` to make safe, architecture-compliant changes |

---

## Scope

### In scope (current or planned in-repo)

| Area | Description |
|------|-------------|
| REST JSON API | ASP.NET Core controllers, Swagger in Development |
| Persistence | EF Core 8, SQL Server, code-first migrations |
| Patterns | Repository, Unit of Work, middleware exception handling |
| Testing | xUnit, InMemory EF, WebApplicationFactory |
| DevOps basics | GitHub Actions CI, Docker Compose for local SQL + API |
| Documentation | AGENTS.md, docs/, Copilot instructions |

### Out of scope (not part of this repo today)

| Area | Reason |
|------|--------|
| Frontend / mobile client | API-only sample |
| Multi-tenancy | Complexity beyond learning goals |
| Production deployment guide | README points to future learning steps |
| Full auth system | Planned extension, not implemented |
| E2E browser tests | Unit + API integration tests only |

---

## Technology stack

| Layer | Technology | Version |
|-------|------------|---------|
| Runtime | .NET | 8.0 |
| Web | ASP.NET Core Web API | 8.0 |
| ORM | Entity Framework Core | 8.0.11 |
| Database | SQL Server / LocalDB | 2022 (Docker image) |
| API docs | Swashbuckle (Swagger UI) | 6.6.2 |
| Health checks | AspNetCore.HealthChecks.SqlServer | 8.0.2 |
| Testing | xUnit, Mvc.Testing, EF InMemory | See UnitTest csproj |
| CI | GitHub Actions | ubuntu-latest, .NET 8.0.x |
| Containers | Docker, docker-compose | SQL + API |

---

## Solution projects

| Project | Assembly role | Key contents |
|---------|---------------|--------------|
| `NewbieCoder.API` | Presentation | `Program.cs`, controllers, middleware, Swagger |
| `NewbieCoder.Core` | Domain + contracts | Entities, DTOs, `ApiResponse`, interfaces, exceptions |
| `NewbieCoder.Infrastructure` | Infrastructure | `AppDbContext`, repositories, migrations, DI |
| `NewbieCoder.UnitTest` | Test | Core, repository, API tests |

---

## Sample domain — Todo Item

The project includes a **TodoItem** entity as a teaching vehicle:

- Fields: `Title`, `IsCompleted`, plus `BaseEntity` audit fields
- Database table `TodoItems` created by migration `InitialCreate`
- **No CRUD HTTP endpoints yet** — implementing them is the primary next exercise

This domain is simple on purpose: agents should not over-engineer it with unnecessary abstractions.

---

## API surface today

| Endpoint | Purpose |
|----------|---------|
| `GET /api/health` | Confirm API process is running (standard envelope) |
| `GET /health` | Confirm SQL Server connectivity (ASP.NET Health Checks) |
| `GET /swagger` | Interactive API docs (Development only) |

See [05-api-contracts.md](05-api-contracts.md) for full contract details.

---

## Configuration environments

| Environment | Config file | Typical database |
|-------------|-------------|------------------|
| Development | `appsettings.Development.json` | LocalDB `(localdb)\mssqllocaldb` |
| Default / Production template | `appsettings.json` | LocalDB (replace in real deployment) |
| Docker | Environment variables in `docker-compose.yml` | SQL Server container `sqlserver` |

---

## Roadmap (suggested learning order)

1. **Service layer + Todo CRUD** — full vertical slice through all layers
2. **FluentValidation** — request validation before service layer
3. **JWT authentication** — secure endpoints, user identity
4. **API versioning** — `Asp.Versioning.Mvc`
5. **Serilog** — structured logging replacing default console logger
6. **Deployment** — Azure App Service, VPS, or Kubernetes (external learning)

When each item is implemented, update:
- Relevant `docs/` files (especially 05, 06, 07)
- [11-decision-log.md](11-decision-log.md) with a new ADR

---

## Success criteria for contributors / agents

A change is successful when:

- Clean Architecture dependency rules are preserved
- `dotnet build` succeeds with no new warnings (ideally)
- `dotnet test` passes all existing and new tests
- Public API changes are documented in `docs/05-api-contracts.md`
- Schema changes include EF migration — see infrastructure instructions
