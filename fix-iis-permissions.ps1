# Script cấp quyền cho Application Pool Identity
# Chạy với quyền Administrator

param(
    [string]$AppPoolName = "quanlyfilesBE",
    [string]$AppPath = "C:\inetpub\wwwroot\quanlyfilesBE",
    [string]$StoragePath = "C:\THIBIDI-STORE\p-TK"
)

Write-Host "=== Cấp quyền cho Application Pool Identity ===" -ForegroundColor Green

# Kiểm tra quyền Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: Script phải chạy với quyền Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell và chọn 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

$appPoolIdentity = "IIS AppPool\$AppPoolName"
Write-Host "Application Pool Identity: $appPoolIdentity" -ForegroundColor Cyan

# Bước 1: Cấp quyền cho thư mục ứng dụng
Write-Host "`n[1/3] Cấp quyền cho thư mục ứng dụng..." -ForegroundColor Yellow
if (Test-Path $AppPath) {
    Write-Host "Đang cấp quyền đọc/ghi cho: $AppPath" -ForegroundColor Cyan
    icacls $AppPath /grant "${appPoolIdentity}:(OI)(CI)(F)" /T
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Đã cấp quyền cho thư mục ứng dụng" -ForegroundColor Green
    } else {
        Write-Host "✗ Có lỗi khi cấp quyền" -ForegroundColor Red
    }
} else {
    Write-Host "✗ Thư mục không tồn tại: $AppPath" -ForegroundColor Red
}

# Bước 2: Cấp quyền cho thư mục logs
Write-Host "`n[2/3] Cấp quyền cho thư mục logs..." -ForegroundColor Yellow
$logsPath = Join-Path $AppPath "logs"
if (-not (Test-Path $logsPath)) {
    New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
    Write-Host "Đã tạo thư mục logs" -ForegroundColor Green
}
Write-Host "Đang cấp quyền ghi cho: $logsPath" -ForegroundColor Cyan
icacls $logsPath /grant "${appPoolIdentity}:(OI)(CI)(F)" /T
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Đã cấp quyền cho thư mục logs" -ForegroundColor Green
} else {
    Write-Host "✗ Có lỗi khi cấp quyền" -ForegroundColor Red
}

# Bước 3: Cấp quyền cho thư mục file storage (nếu có)
Write-Host "`n[3/3] Cấp quyền cho thư mục file storage..." -ForegroundColor Yellow
if (Test-Path $StoragePath) {
    Write-Host "Đang cấp quyền đọc/ghi cho: $StoragePath" -ForegroundColor Cyan
    icacls $StoragePath /grant "${appPoolIdentity}:(OI)(CI)(F)" /T
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Đã cấp quyền cho thư mục file storage" -ForegroundColor Green
    } else {
        Write-Host "✗ Có lỗi khi cấp quyền" -ForegroundColor Red
    }
} else {
    Write-Host "⚠ Thư mục file storage không tồn tại: $StoragePath" -ForegroundColor Yellow
    Write-Host "  (Bạn có thể tạo sau hoặc cập nhật đường dẫn trong appsettings.json)" -ForegroundColor Yellow
}

# Kiểm tra quyền đã được cấp
Write-Host "`n=== Kiểm tra quyền ===" -ForegroundColor Green
Write-Host "Đang kiểm tra quyền cho $appPoolIdentity..." -ForegroundColor Cyan

$acl = Get-Acl $AppPath
$permissions = $acl.Access | Where-Object { $_.IdentityReference -like "*$AppPoolName*" }

if ($permissions) {
    Write-Host "✓ Quyền đã được cấp:" -ForegroundColor Green
    foreach ($perm in $permissions) {
        Write-Host "  - Identity: $($perm.IdentityReference)" -ForegroundColor Cyan
        Write-Host "    Rights: $($perm.FileSystemRights)" -ForegroundColor Cyan
    }
} else {
    Write-Host "✗ Chưa thấy quyền được cấp. Có thể cần chạy lại script." -ForegroundColor Red
}

Write-Host "`n=== Hoàn tất! ===" -ForegroundColor Green
Write-Host "Bây giờ hãy:" -ForegroundColor Yellow
Write-Host "1. Mở IIS Manager" -ForegroundColor Yellow
Write-Host "2. Right-click website 'quanlyfilesBE' → Manage Website → Browse" -ForegroundColor Yellow
Write-Host "3. Hoặc chạy lại Test Connection trong IIS Manager" -ForegroundColor Yellow

