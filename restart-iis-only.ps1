# Script chỉ restart IIS Application Pool (không build)
# Dùng khi đã build và copy files lên server

param(
    [string]$AppPoolName = "quanlyfilesBE"
)

Write-Host "=== Restart IIS Application Pool ===" -ForegroundColor Green

# Kiểm tra quyền Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: Script phải chạy với quyền Administrator!" -ForegroundColor Red
    Write-Host "Chạy lại PowerShell với quyền Administrator" -ForegroundColor Yellow
    exit 1
}

# Import IIS Module
Import-Module WebAdministration -ErrorAction SilentlyContinue

if (-not (Get-Module WebAdministration)) {
    Write-Host "ERROR: Không thể import WebAdministration module!" -ForegroundColor Red
    Write-Host "Hãy cài đặt IIS Management Tools" -ForegroundColor Yellow
    exit 1
}

# Kiểm tra Application Pool có tồn tại không
$appPool = Get-WebAppPoolState -Name $AppPoolName -ErrorAction SilentlyContinue

if (-not $appPool) {
    Write-Host "ERROR: Application Pool '$AppPoolName' không tồn tại!" -ForegroundColor Red
    Write-Host "Các Application Pool có sẵn:" -ForegroundColor Yellow
    Get-WebAppPoolState | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Cyan }
    exit 1
}

# Restart Application Pool
Write-Host "Đang restart Application Pool: $AppPoolName" -ForegroundColor Cyan
Restart-WebAppPool -Name $AppPoolName
Start-Sleep -Seconds 3

# Kiểm tra trạng thái
$poolState = Get-WebAppPoolState -Name $AppPoolName
Write-Host "`nApplication Pool State: $($poolState.Value)" -ForegroundColor $(if ($poolState.Value -eq "Started") { "Green" } else { "Red" })

if ($poolState.Value -eq "Started") {
    Write-Host "`n=== Restart thành công! ===" -ForegroundColor Green
    Write-Host "CORS đã được cập nhật với:" -ForegroundColor Cyan
    Write-Host "  - http://localsite.thibidi.com" -ForegroundColor Cyan
    Write-Host "  - http://localhost:4200" -ForegroundColor Cyan
    Write-Host "  - Exposed Authorization header" -ForegroundColor Cyan
} else {
    Write-Host "`nWARNING: Application Pool không khởi động được!" -ForegroundColor Yellow
    Write-Host "Kiểm tra logs tại: C:\inetpub\wwwroot\quanlyfilesBE\logs" -ForegroundColor Yellow
}

