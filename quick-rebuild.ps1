# Script rebuild nhanh và chạy backend
Write-Host "=== Quick Rebuild Backend ===" -ForegroundColor Green
Write-Host ""

# Build
Write-Host "Building..." -ForegroundColor Cyan
dotnet build --no-incremental

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""
Write-Host "Backend đã được rebuild. Nếu đang chạy dotnet run, nhấn Ctrl+C và chạy lại:" -ForegroundColor Yellow
Write-Host "  dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "Hoặc chạy script rebuild-and-run.ps1 để tự động chạy." -ForegroundColor Yellow


