# 03 — User Flows

This document describes **interaction flows** between clients and the API. Flows marked **(implemented)** exist in code today. Flows marked **(planned)** describe the intended design for upcoming Todo CRUD work.

---

## Actors

| Actor | Description |
|-------|-------------|
| **Client** | Browser (Swagger), REST client, curl, PowerShell, future mobile/web app |
| **API** | `NewbieCoder.API` — controllers, middleware, health checks |
| **Service** | Application/business layer **(planned)** |
| **Repository** | `IRepository<T>` + `IUnitOfWork` |
| **Database** | SQL Server — `NewbieCoderDb` |

---

## Flow 1 — Check API is running **(implemented)**

**Trigger:** Client wants to verify the API process responds.

**Endpoint:** `GET /api/health`

```mermaid
sequenceDiagram
    participant Client
    participant Pipeline as HTTP Pipeline
    participant MW as ExceptionHandlingMiddleware
    participant HC as HealthController

    Client->>Pipeline: GET /api/health
    Pipeline->>MW: invoke
    MW->>HC: Get()
    HC-->>MW: ApiResponse Success("OK", trace)
    MW-->>Client: HTTP 200 application/json
```

**Response body:** Standard envelope — `responseData: "OK"`, `responseStatus.responseCode: "000000"`.

**Notes:**
- Does not verify database connectivity
- Uses standard `ApiResponse<T>` envelope
- Implemented in `HealthController.cs`

---

## Flow 2 — Check database connectivity **(implemented)**

**Trigger:** Monitoring tool or developer verifies SQL Server is reachable.

**Endpoint:** `GET /health`

```mermaid
sequenceDiagram
    participant Client
    participant HC as ASP.NET Health Checks
    participant SQL as SQL Server

    Client->>HC: GET /health
    HC->>SQL: Connection probe (timeout 5s)
    alt Connection succeeds
        SQL-->>HC: OK
        HC-->>Client: HTTP 200 "Healthy"
    else Connection fails
        HC-->>Client: HTTP 503 "Unhealthy"
    end
```

**Notes:**
- Response is **plain text**, not `ApiResponse` JSON
- Registered in `ServiceCollectionExtensions` via `AddSqlServer(connectionString, name: "sqlserver")`
- Fails if LocalDB is not running or migration not applied

---

## Flow 3 — Global error handling **(implemented)**

Applies to all controller endpoints when an exception is thrown.

```mermaid
sequenceDiagram
    participant Client
    participant MW as ExceptionHandlingMiddleware
    participant Handler as Controller / Service

    Client->>MW: HTTP request
    MW->>Handler: await next()
    alt BusinessException thrown
        Handler-->>MW: throw BusinessException(404, "not found")
        MW-->>Client: HTTP 404 JSON ApiResponse fail
    else Unhandled exception
        Handler-->>MW: throw Exception
        MW->>MW: LogError (status >= 500)
        MW-->>Client: HTTP 500 JSON ApiResponse fail
    end
```

**Error JSON shape:**

```json
{
  "success": false,
  "message": "Error description",
  "data": null
}
```

---

## Flow 4 — Create todo **(planned)**

**Endpoint:** `POST /api/todos`

```mermaid
sequenceDiagram
    participant Client
    participant Ctrl as TodosController
    participant Svc as TodoService
    participant Repo as IRepository TodoItem
    participant UoW as IUnitOfWork
    participant DB as SQL Server

    Client->>Ctrl: POST /api/todos { "title": "Learn EF Core" }
    Ctrl->>Svc: CreateAsync(request)
    Svc->>Svc: Validate title (BR-001)
    Svc->>Svc: Build entity (Id, CreatedAt, IsCompleted=false)
    Svc->>Repo: AddAsync(entity)
    Svc->>UoW: SaveChangesAsync()
    UoW->>DB: INSERT INTO TodoItems
    DB-->>Client: HTTP 201 ApiResponse { data: todoResponse }
```

**Expected validations:** BR-001 (title), BR-005 (timestamps)

---

## Flow 5 — List todos with paging **(planned)**

**Endpoint:** `GET /api/todos?page=1&pageSize=10`

```mermaid
sequenceDiagram
    participant Client
    participant Ctrl as TodosController
    participant Svc as TodoService
    participant Repo as IRepository TodoItem

    Client->>Ctrl: GET /api/todos?page=1&pageSize=10
    Ctrl->>Svc: GetPagedAsync(pagingRequest)
    Svc->>Svc: Validate paging (BR-006)
    Svc->>Repo: Query with skip/take
    Repo-->>Client: HTTP 200 ApiResponse { data: pagedResult }
```

Uses existing `PagingRequest` DTO from Core.

---

## Flow 6 — Get todo by id **(planned)**

**Endpoint:** `GET /api/todos/{id}`

- Found → HTTP 200 + `ApiResponse<TodoResponse>`
- Not found → BR-004 → HTTP 404

---

## Flow 7 — Update todo **(planned)**

**Endpoint:** `PUT /api/todos/{id}` or `PATCH /api/todos/{id}`

```mermaid
sequenceDiagram
    participant Client
    participant Svc as TodoService
    participant Repo as IRepository
    participant UoW as IUnitOfWork

    Client->>Svc: PUT /api/todos/{id} { title?, isCompleted? }
    Svc->>Repo: GetByIdAsync(id)
    alt Not found
        Svc-->>Client: 404 BusinessException
    else Found
        Svc->>Svc: Apply changes, set UpdatedAt
        Svc->>Repo: UpdateAsync(entity)
        Svc->>UoW: SaveChangesAsync()
        UoW-->>Client: 200 ApiResponse
    end
```

---

## Flow 8 — Delete todo **(planned)**

**Endpoint:** `DELETE /api/todos/{id}`

- Hard delete per BR-003
- Not found → 404
- Success → HTTP 200 or 204 (choose one convention and document in 05-api-contracts)

---

## Flow 9 — Developer setup **(implemented)**

**Actor:** Developer or agent setting up local environment.

```text
1. Clone repository
2. Run scripts/setup.ps1 (or setup.sh)
   └── dotnet restore → tool restore → build → ef database update
3. dotnet run --project NewbieCoder.API
4. Open Swagger or GET /api/health
5. Optional: GET /health to verify SQL
```

---

## Flow 10 — Docker development **(implemented)**

```text
1. docker compose up -d --build
2. Wait for sqlserver container healthy
3. Run migration from host against localhost:1433
4. Access API at http://localhost:5089/swagger
```

See [README.md](../README.md) for exact connection string and commands.

---

## Client tooling

| Tool | Use case |
|------|----------|
| Swagger UI (`/swagger`) | Manual endpoint testing in Development |
| `NewbieCoder.API.http` | VS / VS Code REST Client snippets |
| `Invoke-RestMethod` / curl | Scripting and smoke tests |
| `WebApplicationFactory` | Automated integration tests |

---

## Agent guidance

- When implementing planned flows, follow sequence: Controller → Service → Repository → UnitOfWork
- Add matching sequence tests in `ApiTests/` and `ServiceTests/`
- Update this document to mark flows as **implemented** and remove **(planned)** label
- Keep `/health` and `/api/health` distinct — do not merge into one endpoint
