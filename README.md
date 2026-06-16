# Newbie Coder API

Web API mẫu theo **Clean Architecture** (.NET 8), dành cho người mới bắt đầu học ASP.NET Core, Entity Framework và cấu trúc dự án nhiều lớp.

---

## Mục lục

1. [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
2. [Cấu trúc dự án](#cấu-trúc-dự-án)
3. [Cài đặt lần đầu](#cài-đặt-lần-đầu)
4. [Chạy API](#chạy-api)
5. [Kiểm tra API hoạt động](#kiểm-tra-api-hoạt-động)
6. [Cơ sở dữ liệu & Migration](#cơ-sở-dữ-liệu--migration)
7. [Chạy bằng Docker](#chạy-bằng-docker)
8. [Chạy unit test](#chạy-unit-test)
9. [Mở bằng Visual Studio](#mở-bằng-visual-studio)
10. [Mở bằng VS Code](#mở-bằng-vs-code)
11. [Xử lý lỗi thường gặp](#xử-lý-lỗi-thường-gặp)
12. [Biến môi trường & cấu hình](#biến-môi-trường--cấu-hình)
13. [CI/CD](#cicd)
14. [Bước học tiếp theo](#bước-học-tiếp-theo)

---

## Yêu cầu hệ thống

| Công cụ | Phiên bản khuyến nghị | Ghi chú |
|---------|----------------------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | **8.0** trở lên | Bắt buộc |
| [Git](https://git-scm.com/) | Mới nhất | Clone repo |
| SQL Server **LocalDB** | Đi kèm Visual Studio | Chạy local (Windows) |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Tùy chọn | Chạy SQL Server + API trong container |
| Visual Studio 2022 **hoặc** VS Code | Tùy chọn | IDE |

Kiểm tra .NET đã cài:

```powershell
dotnet --version
```

Kết quả mong đợi: dòng bắt đầu bằng `8.` (ví dụ `8.0.403`).

---

## Cấu trúc dự án

```text
newbie-coder-api/
│
├── NewbieCoder.API/                 # Lớp HTTP: Controllers, Middleware, Swagger
├── NewbieCoder.Core/                 # Domain: Entities, DTOs, Interfaces
├── NewbieCoder.Infrastructure/        # EF Core, Repository, DbContext, Migrations
├── NewbieCoder.UnitTest/              # Unit & API integration tests
│
├── scripts/
│   ├── setup.ps1                    # Tự động setup (Windows)
│   └── setup.sh                     # Tự động setup (macOS/Linux)
├── .config/dotnet-tools.json        # Công cụ dotnet-ef (phiên bản cố định)
├── Dockerfile
├── docker-compose.yml
├── .github/workflows/ci.yml         # CI GitHub (push → build + test)
└── newbie-coder-api.sln
```

**Luồng phụ thuộc:**

```text
API  →  Infrastructure  →  Core
              ↑
         UnitTest (tham chiếu cả 3 project)
```

- **Core** không phụ thuộc project nào khác.
- **Infrastructure** triển khai database, repository, unit of work.
- **API** là điểm vào, nhận HTTP request.

---

## Cài đặt lần đầu

### Bước 1: Clone repository

```powershell
git clone <url-repo-cua-ban>
cd newbie-coder-api
```

### Bước 2: Chạy script setup (khuyến nghị)

**Windows (PowerShell):**

```powershell
.\scripts\setup.ps1
```

Script sẽ: restore package → cài tool `dotnet-ef` → build → tạo/cập nhật database trên LocalDB.

**macOS / Linux:**

```bash
chmod +x scripts/setup.sh
./scripts/setup.sh
```

### Bước 3: Setup thủ công (nếu script lỗi)

```powershell
dotnet restore newbie-coder-api.sln
dotnet tool restore
dotnet build newbie-coder-api.sln
dotnet ef database update --project NewbieCoder.Infrastructure --startup-project NewbieCoder.API
```

> Lệnh `dotnet ef` dùng tool trong `.config/dotnet-tools.json`, không cần `dotnet tool install -g` (trừ khi bạn muốn dùng global).

---

## Chạy API

```powershell
dotnet run --project NewbieCoder.API
```

Hoặc trong Visual Studio: chọn project **NewbieCoder.API** → nhấn **F5**.

Sau khi chạy, terminal hiển thị URL, ví dụ:

| Môi trường | URL |
|------------|-----|
| HTTPS | `https://localhost:7020` |
| HTTP | `http://localhost:5029` |
| Swagger UI | `https://localhost:7020/swagger` |

> Cổng có thể khác — xem file `NewbieCoder.API/Properties/launchSettings.json`.

---

## Kiểm tra API hoạt động

### 1. Swagger (trình duyệt)

Mở: **https://localhost:7020/swagger**

Thử endpoint **GET /api/Health** → phản hồi JSON dạng:

```json
{
  "requestTrace": "...",
  "responseDateTime": "2026-06-12T22:22:05+07:00",
  "responseData": "OK",
  "responseStatus": {
    "responseCode": "000000",
    "responseMessage": "Success",
    "tracingMessage": null
  }
}
```

### 2. Health check (database)

```text
GET https://localhost:7020/health
```

- Trả `Healthy` nếu kết nối SQL thành công.
- Trả `Unhealthy` nếu chưa cài LocalDB hoặc chưa chạy `database update`.

### 3. curl / PowerShell

```powershell
Invoke-RestMethod https://localhost:7020/api/health
```

---

## Cơ sở dữ liệu & Migration

### Chuỗi kết nối mặc định (Development)

File: `NewbieCoder.API/appsettings.Development.json`

```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NewbieCoderDb;..."
```

Dùng **SQL Server LocalDB** trên Windows (thường có khi cài Visual Studio).

### Bảng mẫu

Migration `InitialCreate` tạo bảng **TodoItems** (entity mẫu để học EF Core).

### Lệnh migration thường dùng

| Việc cần làm | Lệnh |
|--------------|------|
| Áp dụng migration lên DB | `dotnet ef database update --project NewbieCoder.Infrastructure --startup-project NewbieCoder.API` |
| Tạo migration mới | `dotnet ef migrations add TenMigration --project NewbieCoder.Infrastructure --startup-project NewbieCoder.API --output-dir Data/Migrations` |
| Xóa migration cuối (chưa apply) | `dotnet ef migrations remove --project NewbieCoder.Infrastructure --startup-project NewbieCoder.API` |

### Cài LocalDB (nếu chưa có)

1. Mở **Visual Studio Installer**
2. Sửa bản cài Visual Studio → chọn workload **ASP.NET and web development**
3. Trong **Individual components**, tick **SQL Server Express LocalDB**

Hoặc cài [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads) và đổi connection string trong `appsettings.Development.json`.

---

## Chạy bằng Docker

Yêu cầu: **Docker Desktop** đang chạy.

### 1. Khởi động SQL Server + API

```powershell
docker compose up -d --build
```

| Dịch vụ | Cổng | Mô tả |
|---------|------|--------|
| SQL Server | `localhost:1433` | SA password: `NewbieCoder@2024!` (chỉ dev) |
| API | `http://localhost:5089` | Swagger: `http://localhost:5089/swagger` |

### 2. Tạo database trong Docker (chạy từ máy host)

Sau khi container `sqlserver` đã **healthy**:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=NewbieCoderDb;User Id=sa;Password=NewbieCoder@2024!;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet ef database update --project NewbieCoder.Infrastructure --startup-project NewbieCoder.API
```

### 3. Dừng Docker

```powershell
docker compose down
```

Xóa cả dữ liệu SQL (volume):

```powershell
docker compose down -v
```

> **Lưu ý:** Mật khẩu trong `docker-compose.yml` chỉ dùng cho môi trường học tập. Không dùng trên production.

---

## Chạy unit test

```powershell
dotnet test
```

Cấu trúc test:

| Thư mục | Nội dung |
|---------|----------|
| `CoreTests/` | Test logic Core (ApiResponse, …) |
| `RepositoryTests/` | Test DbContext / UnitOfWork (InMemory) |
| `ServiceTests/` | Test service (mở rộng sau) |
| `ApiTests/` | Test HTTP qua `WebApplicationFactory` |

Kết quả mong đợi: **Passed** (tất cả test).

---

## Mở bằng Visual Studio

1. Mở file `newbie-coder-api.sln`
2. Trong **Solution Explorer**, chuột phải **NewbieCoder.API** → **Set as Startup Project**
3. Chọn profile **https** trên toolbar
4. Nhấn **F5** (Debug) hoặc **Ctrl+F5** (không debug)

Lần đầu: chạy `.\scripts\setup.ps1` hoặc **Package Manager Console**:

```powershell
Update-Database -Project NewbieCoder.Infrastructure -StartupProject NewbieCoder.API
```

---

## Mở bằng VS Code

1. Cài extension **C# Dev Kit** (hoặc C#)
2. Mở thư mục `newbie-coder-api`
3. Terminal → chạy `.\scripts\setup.ps1`
4. **Run and Debug** (F5) — chọn **.NET Core Launch (web)** nếu có `launch.json`, hoặc:

```powershell
dotnet run --project NewbieCoder.API
```

Tạo `.vscode/launch.json` (tùy chọn):

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "NewbieCoder.API",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/NewbieCoder.API/bin/Debug/net8.0/NewbieCoder.API.dll",
      "args": [],
      "cwd": "${workspaceFolder}/NewbieCoder.API",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
        "uriFormat": "%s/swagger"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

---

## Xử lý lỗi thường gặp

### `dotnet ef` không được nhận diện

```powershell
dotnet tool restore
dotnet ef --version
```

### Lỗi kết nối SQL / LocalDB

- Kiểm tra LocalDB: `sqllocaldb info` → `sqllocaldb start MSSQLLocalDB`
- Chạy lại: `dotnet ef database update ...`
- Hoặc dùng Docker (mục [Chạy bằng Docker](#chạy-bằng-docker))

### Certificate HTTPS (trình duyệt cảnh báo)

Development dùng chứng chỉ tự ký. Chạy một lần:

```powershell
dotnet dev-certs https --trust
```

### Cổng đã được sử dụng

Đổi cổng trong `launchSettings.json` hoặc tắt process đang chiếm cổng 5029/7020.

### Docker: API Unhealthy

1. `docker compose ps` — đợi `sqlserver` healthy
2. Chạy migration từ host (mục Docker bước 2)
3. `docker compose logs api`

### Build lỗi sau khi pull code mới

```powershell
dotnet clean
dotnet restore
dotnet build
```

---

## Biến môi trường & cấu hình

| Biến | Ví dụ | Mô tả |
|------|-------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | Chọn file `appsettings.{Environment}.json` |
| `ConnectionStrings__DefaultConnection` | Chuỗi SQL | Ghi đè connection string (Docker, production) |

Thứ tự ưu tiên cấu hình ASP.NET Core (cao → thấp):

1. Biến môi trường
2. User Secrets (Development, project API)
3. `appsettings.{Environment}.json`
4. `appsettings.json`

---

## CI/CD

File `.github/workflows/ci.yml` chạy khi push/PR lên `main`, `master`, `develop`:

- `dotnet tool restore`
- `dotnet restore`
- `dotnet build` (Release)
- `dotnet test`

Không cần cấu hình thêm; khi bạn đẩy code lên GitHub, Actions sẽ tự chạy.

---

## Bước học tiếp theo

Gợi ý mở rộng dự án (theo thứ tự):

1. Thêm **Service layer** + CRUD API cho `TodoItem`
2. **FluentValidation** cho request DTO
3. **JWT Authentication** + Authorization
4. **API Versioning** (`Asp.Versioning.Mvc`)
5. **Serilog** structured logging
6. Triển khai production (Azure App Service, VPS, Kubernetes)

---

## Tài liệu tham khảo

- [ASP.NET Core documentation](https://learn.microsoft.com/aspnet/core/)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [.NET CLI](https://learn.microsoft.com/dotnet/core/tools/)

---

**Chúc bạn học tốt.** Nếu gặp lỗi không có trong mục [Xử lý lỗi](#xử-lý-lỗi-thường-gặp), mở issue kèm log terminal và phiên bản `dotnet --info`.
