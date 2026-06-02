# Thiết lập lần đầu cho Windows (PowerShell)
# Chạy: .\scripts\setup.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

Write-Host "=== Newbie Coder API - Setup ===" -ForegroundColor Cyan

Write-Host "`n[1/5] Kiem tra .NET SDK..." -ForegroundColor Yellow
$version = dotnet --version
Write-Host "      SDK: $version"
if (-not ($version -match "^8\.")) {
    Write-Warning "Khuyen nghi dung .NET 8 SDK. Tai: https://dotnet.microsoft.com/download/dotnet/8.0"
}

Write-Host "`n[2/5] Restore packages..." -ForegroundColor Yellow
dotnet restore newbie-coder-api.sln

Write-Host "`n[3/5] Restore dotnet tools (EF)..." -ForegroundColor Yellow
dotnet tool restore

Write-Host "`n[4/5] Build solution..." -ForegroundColor Yellow
dotnet build newbie-coder-api.sln --configuration Debug

Write-Host "`n[5/5] Cap nhat database (LocalDB)..." -ForegroundColor Yellow
try {
    dotnet ef database update --project NewbieCoder.Infrastructure --startup-project NewbieCoder.API
    Write-Host "      Database da cap nhat thanh cong." -ForegroundColor Green
}
catch {
    Write-Warning "Khong cap nhat duoc DB. Kiem tra SQL Server LocalDB da cai chua."
    Write-Host "      Thu cai: Visual Studio Installer -> SQL Server Express LocalDB" -ForegroundColor Gray
    Write-Host "      Hoac chay bang Docker: docker compose up -d" -ForegroundColor Gray
}

Write-Host "`n=== Hoan tat ===" -ForegroundColor Cyan
Write-Host "Chay API:  dotnet run --project NewbieCoder.API"
Write-Host "Swagger:   https://localhost:7082/swagger (xem launchSettings.json)"
Write-Host "Health:    https://localhost:7082/health"
Write-Host "Test:      dotnet test"
