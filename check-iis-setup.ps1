# Script kiểm tra toàn bộ cấu hình IIS cho .NET Core
# Chạy với quyền Administrator

param(
    [string]$AppPoolName = "quanlyfilesBE",
    [string]$SiteName = "quanlyfilesBE",
    [string]$PublishPath = "C:\inetpub\wwwroot\quanlyfilesBE"
)

Write-Host "=== Kiểm Tra Cấu Hình IIS cho .NET Core ===" -ForegroundColor Green
Write-Host ""

$allChecksPassed = $true

# 1. Kiểm tra .NET 9.0 Runtime
Write-Host "[1/8] Kiểm tra .NET 9.0 Runtime..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ .NET Runtime: $dotnetVersion" -ForegroundColor Green
        if ($dotnetVersion -notlike "9.*") {
            Write-Host "⚠ Cảnh báo: Không phải .NET 9.0" -ForegroundColor Yellow
        }
    } else {
        Write-Host "✗ .NET Runtime chưa được cài đặt" -ForegroundColor Red
        $allChecksPassed = $false
    }
} catch {
    Write-Host "✗ Không thể kiểm tra .NET Runtime" -ForegroundColor Red
    $allChecksPassed = $false
}

# 2. Kiểm tra ASP.NET Core Module
Write-Host "`n[2/8] Kiểm tra ASP.NET Core Module..." -ForegroundColor Yellow
$modulePath = "C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
if (Test-Path $modulePath) {
    Write-Host "✓ ASP.NET Core Module V2 đã được cài đặt" -ForegroundColor Green
} else {
    Write-Host "✗ ASP.NET Core Module chưa được cài đặt" -ForegroundColor Red
    Write-Host "  Tải và cài đặt .NET 9.0 Hosting Bundle từ:" -ForegroundColor Yellow
    Write-Host "  https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Cyan
    $allChecksPassed = $false
}

# 3. Kiểm tra Application Pool
Write-Host "`n[3/8] Kiểm tra Application Pool..." -ForegroundColor Yellow
Import-Module WebAdministration -ErrorAction SilentlyContinue
$appPool = Get-WebAppPoolState -Name $AppPoolName -ErrorAction SilentlyContinue

if ($appPool) {
    Write-Host "✓ Application Pool '$AppPoolName' tồn tại" -ForegroundColor Green
    
    # Kiểm tra .NET CLR Version
    $runtimeVersion = (Get-ItemProperty "IIS:\AppPools\$AppPoolName").managedRuntimeVersion
    if ([string]::IsNullOrEmpty($runtimeVersion)) {
        Write-Host "✓ .NET CLR Version: No Managed Code (đúng)" -ForegroundColor Green
    } else {
        Write-Host "✗ .NET CLR Version: $runtimeVersion (sai - phải là 'No Managed Code')" -ForegroundColor Red
        $allChecksPassed = $false
    }
    
    # Kiểm tra trạng thái
    Write-Host "  Trạng thái: $($appPool.Value)" -ForegroundColor Cyan
} else {
    Write-Host "✗ Application Pool '$AppPoolName' không tồn tại" -ForegroundColor Red
    $allChecksPassed = $false
}

# 4. Kiểm tra Website
Write-Host "`n[4/8] Kiểm tra Website..." -ForegroundColor Yellow
$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue

if ($site) {
    Write-Host "✓ Website '$SiteName' tồn tại" -ForegroundColor Green
    Write-Host "  Physical Path: $($site.physicalPath)" -ForegroundColor Cyan
    Write-Host "  State: $($site.State)" -ForegroundColor Cyan
} else {
    Write-Host "✗ Website '$SiteName' không tồn tại" -ForegroundColor Red
    $allChecksPassed = $false
}

# 5. Kiểm tra thư mục publish
Write-Host "`n[5/8] Kiểm tra thư mục publish..." -ForegroundColor Yellow
if (Test-Path $PublishPath) {
    Write-Host "✓ Thư mục publish tồn tại: $PublishPath" -ForegroundColor Green
    
    # Kiểm tra file DLL
    $dllPath = Join-Path $PublishPath "quanlyfilesBE.dll"
    if (Test-Path $dllPath) {
        Write-Host "✓ File quanlyfilesBE.dll tồn tại" -ForegroundColor Green
    } else {
        Write-Host "✗ File quanlyfilesBE.dll không tồn tại" -ForegroundColor Red
        $allChecksPassed = $false
    }
} else {
    Write-Host "✗ Thư mục publish không tồn tại: $PublishPath" -ForegroundColor Red
    $allChecksPassed = $false
}

# 6. Kiểm tra web.config
Write-Host "`n[6/8] Kiểm tra web.config..." -ForegroundColor Yellow
$webConfigPath = Join-Path $PublishPath "web.config"

if (Test-Path $webConfigPath) {
    Write-Host "✓ File web.config tồn tại" -ForegroundColor Green
    
    # Kiểm tra cú pháp XML
    try {
        [xml]$xml = Get-Content $webConfigPath
        Write-Host "✓ Cú pháp XML hợp lệ" -ForegroundColor Green
        
        # Kiểm tra các thẻ quan trọng
        $hasHandler = $xml.configuration.location.'system.webServer'.handlers.add | Where-Object { $_.name -eq "aspNetCore" }
        $hasAspNetCore = $xml.configuration.location.'system.webServer'.aspNetCore
        
        if ($hasHandler -and $hasAspNetCore) {
            Write-Host "✓ Cấu hình hợp lệ (có handler và aspNetCore)" -ForegroundColor Green
        } else {
            Write-Host "✗ Cấu hình thiếu thành phần" -ForegroundColor Red
            $allChecksPassed = $false
        }
    } catch {
        Write-Host "✗ Lỗi cú pháp XML: $_" -ForegroundColor Red
        $allChecksPassed = $false
    }
    
    # Kiểm tra BOM
    $bytes = [System.IO.File]::ReadAllBytes($webConfigPath)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        Write-Host "⚠ File có BOM (nên loại bỏ)" -ForegroundColor Yellow
    } else {
        Write-Host "✓ File không có BOM" -ForegroundColor Green
    }
} else {
    Write-Host "✗ File web.config không tồn tại" -ForegroundColor Red
    Write-Host "  Chạy fix-webconfig.ps1 để tạo file" -ForegroundColor Yellow
    $allChecksPassed = $false
}

# 7. Kiểm tra quyền truy cập
Write-Host "`n[7/8] Kiểm tra quyền truy cập..." -ForegroundColor Yellow
$appPoolIdentity = "IIS AppPool\$AppPoolName"

try {
    $acl = Get-Acl $PublishPath
    $permissions = $acl.Access | Where-Object { $_.IdentityReference -like "*$AppPoolName*" }
    
    if ($permissions) {
        Write-Host "✓ Quyền đã được cấp cho $appPoolIdentity" -ForegroundColor Green
    } else {
        Write-Host "⚠ Quyền chưa được cấp cho $appPoolIdentity" -ForegroundColor Yellow
        Write-Host "  Chạy fix-iis-permissions.ps1 để cấp quyền" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠ Không thể kiểm tra quyền: $_" -ForegroundColor Yellow
}

# 8. Kiểm tra thư mục logs
Write-Host "`n[8/8] Kiểm tra thư mục logs..." -ForegroundColor Yellow
$logsPath = Join-Path $PublishPath "logs"
if (Test-Path $logsPath) {
    Write-Host "✓ Thư mục logs tồn tại" -ForegroundColor Green
    
    # Kiểm tra file log mới nhất
    $latestLog = Get-ChildItem $logsPath -Filter "stdout_*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latestLog) {
        Write-Host "  Log mới nhất: $($latestLog.Name) ($($latestLog.LastWriteTime))" -ForegroundColor Cyan
    }
} else {
    Write-Host "⚠ Thư mục logs chưa tồn tại" -ForegroundColor Yellow
    Write-Host "  Sẽ được tạo tự động khi ứng dụng chạy" -ForegroundColor Cyan
}

# Tổng kết
Write-Host "`n=== Kết Quả Kiểm Tra ===" -ForegroundColor Green
if ($allChecksPassed) {
    Write-Host "✓ Tất cả kiểm tra đều PASS!" -ForegroundColor Green
    Write-Host "`nNếu vẫn gặp lỗi, thử:" -ForegroundColor Yellow
    Write-Host "1. Restart Application Pool trong IIS Manager" -ForegroundColor Yellow
    Write-Host "2. Restart IIS: iisreset" -ForegroundColor Yellow
    Write-Host "3. Kiểm tra Event Viewer để xem lỗi chi tiết" -ForegroundColor Yellow
} else {
    Write-Host "✗ Có một số vấn đề cần sửa!" -ForegroundColor Red
    Write-Host "`nCác bước khắc phục:" -ForegroundColor Yellow
    Write-Host "1. Chạy fix-webconfig.ps1 để sửa web.config" -ForegroundColor Yellow
    Write-Host "2. Chạy fix-iis-permissions.ps1 để cấp quyền" -ForegroundColor Yellow
    Write-Host "3. Đảm bảo .NET 9.0 Hosting Bundle đã được cài đặt" -ForegroundColor Yellow
}

