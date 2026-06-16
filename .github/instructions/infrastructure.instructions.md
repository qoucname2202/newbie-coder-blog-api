---
applyTo: "NewbieCoder.Infrastructure/**"
---

# Infrastructure Layer — NewbieCoder.Infrastructure

## Purpose

The Infrastructure layer **implements persistence and external technical concerns**. It is the only layer that references Entity Framework Core and SQL Server. It implements interfaces defined in Core.

## Project structure

```text
NewbieCoder.Infrastructure/
├── Data/
│   ├── AppDbContext.cs
│   ├── Configurations/
│   │   └── TodoItemConfiguration.cs
│   └── Migrations/
│       ├── 20260520151810_InitialCreate.cs
│       ├── 20260520151810_InitialCreate.Designer.cs
│       └── AppDbContextModelSnapshot.cs
├── Repositories/
│   └── Repository.cs              # Generic IRepository<T>
├── UnitOfWork/
│   └── UnitOfWork.cs              # EfUnitOfWork
├── DependencyInjection.cs         # AddInfrastructure()
└── NewbieCoder.Infrastructure.csproj
```

## DependencyInjection — AddInfrastructure

File: `DependencyInjection.cs`

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

    services.AddScoped<IUnitOfWork, EfUnitOfWork>();
    return services;
}
```

Key points:
- Connection string key: `DefaultConnection`
- Migrations assembly explicitly set to Infrastructure (migrations live here, not in API)
- `IUnitOfWork` registered as **Scoped** (same scope as DbContext per request)

When adding entity-specific repositories:
```csharp
services.AddScoped<ITodoRepository, TodoRepository>();
```

## AppDbContext

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

Rules:
- One `DbSet<T>` per aggregate root / entity
- All Fluent configurations discovered via `ApplyConfigurationsFromAssembly`
- Do not configure entities inline in `OnModelCreating` unless one-off and documented

## Entity configuration — Fluent API

File pattern: `Data/Configurations/{Entity}Configuration.cs`

```csharp
public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable("TodoItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.IsCompleted).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.IsCompleted);
    }
}
```

When adding a new entity:
1. Create entity in Core
2. Create configuration class here
3. Add `DbSet` to `AppDbContext`
4. Generate migration

## Generic Repository&lt;T&gt;

File: `Repositories/Repository.cs`

```csharp
public class Repository<T>(AppDbContext context) : IRepository<T> where T : BaseEntity
```

| Method | EF behavior |
|--------|-------------|
| `GetByIdAsync` | `DbSet.FindAsync` — tracked |
| `GetAllAsync` | `AsNoTracking().ToListAsync` |
| `FindAsync` | `AsNoTracking().Where(predicate).ToListAsync` |
| `AddAsync` | `DbSet.AddAsync` |
| `UpdateAsync` | `DbSet.Update` |
| `DeleteAsync` | `DbSet.Remove` |

Design notes:
- Read methods use `AsNoTracking()` for performance
- Write methods only stage changes — `SaveChangesAsync` is UnitOfWork responsibility
- For complex queries, prefer a dedicated repository interface rather than leaking `IQueryable` from generic repo

## EfUnitOfWork

File: `UnitOfWork/UnitOfWork.cs`

```csharp
public class EfUnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
```

One DbContext instance per scope — UnitOfWork and repositories must share the same scoped `AppDbContext`.

## Migrations

### Commands (run from repo root)

```powershell
dotnet tool restore

# Apply all pending migrations
dotnet ef database update `
  --project NewbieCoder.Infrastructure `
  --startup-project NewbieCoder.API

# Create new migration
dotnet ef migrations add DescriptiveName `
  --project NewbieCoder.Infrastructure `
  --startup-project NewbieCoder.API `
  --output-dir Data/Migrations

# Remove last migration (only if NOT applied to DB)
dotnet ef migrations remove `
  --project NewbieCoder.Infrastructure `
  --startup-project NewbieCoder.API
```

### Migration file rules

- Output directory: always `Data/Migrations/`
- Tool version pinned in `.config/dotnet-tools.json`
- Do not manually edit `.Designer.cs` or `AppDbContextModelSnapshot.cs` except merge conflict resolution
- Migration names should describe the change: `AddTodoPriorityColumn`, not `Migration1`

### Current schema — TodoItems

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uniqueidentifier | PK |
| Title | nvarchar(200) | NOT NULL |
| IsCompleted | bit | NOT NULL |
| CreatedAt | datetime2 | NOT NULL |
| UpdatedAt | datetime2 | NULL |

Index: `IX_TodoItems_IsCompleted`

## Connection strings

Default (LocalDB Windows):
```text
Server=(localdb)\mssqllocaldb;Database=NewbieCoderDb;Trusted_Connection=True;...
```

Docker (from host to apply migrations):
```text
Server=localhost,1433;Database=NewbieCoderDb;User Id=sa;Password=...;TrustServerCertificate=True;...
```

Docker (API container internal):
```text
Server=sqlserver;Database=NewbieCoderDb;User Id=sa;Password=...;...
```

## Packages

| Package | Purpose |
|---------|---------|
| Microsoft.EntityFrameworkCore | ORM core |
| Microsoft.EntityFrameworkCore.SqlServer | SQL Server provider |

EF Design package is referenced by API project for CLI tooling only.

## Testing Infrastructure code

Use EF Core InMemory provider in tests — see `NewbieCoder.UnitTest/RepositoryTests/UnitOfWorkTests.cs`.

Do not use InMemory provider for testing SQL-specific behavior (constraints, raw SQL) — use integration tests against real SQL if needed later.
