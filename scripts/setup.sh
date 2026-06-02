#!/usr/bin/env bash
# Thiết lập lần đầu (macOS / Linux)
# Chạy: chmod +x scripts/setup.sh && ./scripts/setup.sh

set -euo pipefail
cd "$(dirname "$0")/.."

echo "=== Newbie Coder API - Setup ==="

echo ""
echo "[1/5] Kiểm tra .NET SDK..."
dotnet --version

echo ""
echo "[2/5] Restore packages..."
dotnet restore newbie-coder-api.sln

echo ""
echo "[3/5] Restore dotnet tools (EF)..."
dotnet tool restore

echo ""
echo "[4/5] Build solution..."
dotnet build newbie-coder-api.sln -c Debug

echo ""
echo "[5/5] Cập nhật database..."
if dotnet ef database update --project NewbieCoder.Infrastructure --startup-project NewbieCoder.API; then
  echo "      Database đã cập nhật thành công."
else
  echo "      Không cập nhật được DB. Dùng Docker: docker compose up -d"
fi

echo ""
echo "=== Hoàn tất ==="
echo "Chạy API:  dotnet run --project NewbieCoder.API"
echo "Test:      dotnet test"
