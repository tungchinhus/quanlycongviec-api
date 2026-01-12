# Script để chạy EF Core migration cho TechnicalSheetApproval
# Lưu ý: Backend phải được dừng trước khi chạy migration

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Running EF Core Migration" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Kiểm tra xem backend có đang chạy không
$backendProcess = Get-Process -Name "quanlyfilesBE" -ErrorAction SilentlyContinue
if ($backendProcess) {
    Write-Host "WARNING: Backend process is running (PID: $($backendProcess.Id))" -ForegroundColor Yellow
    Write-Host "Please stop the backend before running migration." -ForegroundColor Yellow
    Write-Host ""
    $continue = Read-Host "Do you want to continue anyway? (Y/N)"
    if ($continue -ne "Y" -and $continue -ne "y") {
        Write-Host "Migration cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# Chuyển đến thư mục project
$projectPath = Join-Path $PSScriptRoot ".."
Set-Location $projectPath

Write-Host "Running: dotnet ef database update" -ForegroundColor Green
Write-Host ""

try {
    dotnet ef database update
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "=========================================" -ForegroundColor Green
        Write-Host "EF Core migration completed successfully!" -ForegroundColor Green
        Write-Host "=========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "The TechnicalSheetApproval table is now ready to use." -ForegroundColor Cyan
    } else {
        Write-Host ""
        Write-Host "ERROR: Migration failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "ERROR: Failed to run migration" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
