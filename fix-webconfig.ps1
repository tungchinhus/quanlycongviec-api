# Script sửa lỗi web.config trong thư mục publish
# Chạy với quyền Administrator (nếu cần)

param(
    [string]$PublishPath = "C:\inetpub\wwwroot\quanlyfilesBE",
    [string]$Environment = "Production"
)

Write-Host "=== Sửa lỗi web.config ===" -ForegroundColor Green

# Kiểm tra ASP.NET Core Module
Write-Host "`n[0/4] Kiểm tra ASP.NET Core Module..." -ForegroundColor Yellow
$aspNetCoreModule = Get-WindowsFeature -Name IIS-ASPNET45 -ErrorAction SilentlyContinue
if (-not $aspNetCoreModule) {
    Write-Host "⚠ Cảnh báo: Không tìm thấy ASP.NET Core Module" -ForegroundColor Yellow
    Write-Host "  Hãy cài đặt .NET 9.0 Hosting Bundle từ:" -ForegroundColor Yellow
    Write-Host "  https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Cyan
} else {
    Write-Host "✓ ASP.NET Core Module đã được cài đặt" -ForegroundColor Green
}

$webConfigPath = Join-Path $PublishPath "web.config"

# Kiểm tra thư mục publish có tồn tại không
if (-not (Test-Path $PublishPath)) {
    Write-Host "ERROR: Thư mục publish không tồn tại: $PublishPath" -ForegroundColor Red
    Write-Host "Hãy publish application trước!" -ForegroundColor Yellow
    exit 1
}

# Tạo web.config mới với cấu hình đúng
Write-Host "`n[1/4] Đang tạo/cập nhật web.config..." -ForegroundColor Yellow

$webConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" 
                  arguments=".\quanlyfilesBE.dll" 
                  stdoutLogEnabled="true" 
                  stdoutLogFile=".\logs\stdout" 
                  hostingModel="inprocess"
                  requestTimeout="00:20:00">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="$Environment" />
        </environmentVariables>
      </aspNetCore>
      <httpErrors errorMode="Detailed" />
    </system.webServer>
  </location>
</configuration>
"@

# Backup web.config cũ nếu có
if (Test-Path $webConfigPath) {
    $backupPath = "$webConfigPath.backup.$(Get-Date -Format 'yyyyMMddHHmmss')"
    Copy-Item $webConfigPath $backupPath -Force
    Write-Host "Đã backup web.config cũ: $backupPath" -ForegroundColor Yellow
}

# Ghi file web.config mới với UTF-8 without BOM
try {
    # Sử dụng UTF8Encoding với BOM = false để tránh lỗi
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($webConfigPath, $webConfigContent, $utf8NoBom)
    Write-Host "✓ Đã tạo/cập nhật web.config thành công!" -ForegroundColor Green
} catch {
    Write-Host "✗ Lỗi khi tạo web.config: $_" -ForegroundColor Red
    exit 1
}

# Kiểm tra cú pháp XML
Write-Host "`n[2/4] Đang kiểm tra cú pháp XML..." -ForegroundColor Yellow
try {
    [xml]$xml = Get-Content $webConfigPath
    Write-Host "✓ Cú pháp XML hợp lệ!" -ForegroundColor Green
} catch {
    Write-Host "✗ Lỗi cú pháp XML: $_" -ForegroundColor Red
    exit 1
}

# Kiểm tra các thẻ quan trọng
Write-Host "`n[3/4] Đang kiểm tra cấu hình..." -ForegroundColor Yellow
$hasHandler = $xml.configuration.location.'system.webServer'.handlers.add | Where-Object { $_.name -eq "aspNetCore" }
$hasAspNetCore = $xml.configuration.location.'system.webServer'.aspNetCore

if ($hasHandler -and $hasAspNetCore) {
    Write-Host "✓ Cấu hình hợp lệ!" -ForegroundColor Green
    Write-Host "  - Handler: aspNetCore" -ForegroundColor Cyan
    Write-Host "  - Process Path: $($hasAspNetCore.processPath)" -ForegroundColor Cyan
    Write-Host "  - Arguments: $($hasAspNetCore.arguments)" -ForegroundColor Cyan
    Write-Host "  - Hosting Model: $($hasAspNetCore.hostingModel)" -ForegroundColor Cyan
    Write-Host "  - Environment: $($hasAspNetCore.environmentVariables.environmentVariable.value)" -ForegroundColor Cyan
} else {
    Write-Host "✗ Cấu hình thiếu một số thành phần!" -ForegroundColor Red
    exit 1
}

# Kiểm tra file có tồn tại và có thể đọc được
Write-Host "`n[4/4] Kiểm tra file cuối cùng..." -ForegroundColor Yellow
if (Test-Path $webConfigPath) {
    $fileInfo = Get-Item $webConfigPath
    Write-Host "✓ File tồn tại: $webConfigPath" -ForegroundColor Green
    Write-Host "  Kích thước: $($fileInfo.Length) bytes" -ForegroundColor Cyan
    Write-Host "  Ngày tạo: $($fileInfo.CreationTime)" -ForegroundColor Cyan
    
    # Kiểm tra encoding
    $bytes = [System.IO.File]::ReadAllBytes($webConfigPath)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        Write-Host "⚠ Cảnh báo: File có BOM (Byte Order Mark)" -ForegroundColor Yellow
        Write-Host "  Đang tạo lại file không có BOM..." -ForegroundColor Cyan
        $content = [System.IO.File]::ReadAllText($webConfigPath, [System.Text.Encoding]::UTF8)
        $utf8NoBom = New-Object System.Text.UTF8Encoding $false
        [System.IO.File]::WriteAllText($webConfigPath, $content, $utf8NoBom)
        Write-Host "✓ Đã loại bỏ BOM" -ForegroundColor Green
    } else {
        Write-Host "✓ File không có BOM (đúng)" -ForegroundColor Green
    }
} else {
    Write-Host "✗ File không tồn tại!" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Hoàn tất! ===" -ForegroundColor Green
Write-Host "File web.config đã được sửa tại: $webConfigPath" -ForegroundColor Cyan
Write-Host "`nBây giờ hãy:" -ForegroundColor Yellow
Write-Host "1. Mở IIS Manager" -ForegroundColor Yellow
Write-Host "2. Restart Application Pool 'quanlyfilesBE'" -ForegroundColor Yellow
Write-Host "3. Refresh website và kiểm tra lại" -ForegroundColor Yellow
Write-Host "`nNếu vẫn còn lỗi, kiểm tra:" -ForegroundColor Yellow
Write-Host "- .NET 9.0 Hosting Bundle đã được cài đặt chưa" -ForegroundColor Yellow
Write-Host "- Application Pool đang dùng .NET CLR Version = 'No Managed Code'" -ForegroundColor Yellow
Write-Host "- Quyền truy cập thư mục (chạy fix-iis-permissions.ps1)" -ForegroundColor Yellow

