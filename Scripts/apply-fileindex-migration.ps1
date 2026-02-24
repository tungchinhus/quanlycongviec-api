# Script chạy migration để tạo bảng FileIndex
# Chạy: .\Scripts\apply-fileindex-migration.ps1

$ErrorActionPreference = "Stop"

Write-Host "Đang apply migration AddFileIndexTable..." -ForegroundColor Yellow

$projectPath = Join-Path $PSScriptRoot ".."
Set-Location $projectPath

# Kiểm tra dotnet ef tools
$efInstalled = dotnet ef --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Đang cài đặt dotnet ef tools..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}

# Chạy migration
Write-Host "Chạy: dotnet ef database update..." -ForegroundColor Cyan
dotnet ef database update --context ApplicationDbContext

if ($LASTEXITCODE -eq 0) {
    Write-Host "Migration đã được apply thành công! Bảng FileIndex đã được tạo." -ForegroundColor Green
} else {
    Write-Host "Lỗi khi chạy migration. Kiểm tra connection string và đảm bảo backend không đang chạy." -ForegroundColor Red
    Write-Host "Hoặc chạy script SQL trực tiếp: Scripts\create-fileindex-table.sql" -ForegroundColor Yellow
}
