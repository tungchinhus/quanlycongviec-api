# Script tạo lại web.config từ đầu
# Chạy với quyền Administrator (nếu cần)

param(
    [string]$PublishPath = "C:\inetpub\wwwroot\quanlyfilesBE",
    [string]$Environment = "Production"
)

Write-Host "=== Tạo lại web.config ===" -ForegroundColor Green

$webConfigPath = Join-Path $PublishPath "web.config"

# Kiểm tra thư mục publish
if (-not (Test-Path $PublishPath)) {
    Write-Host "ERROR: Thư mục publish không tồn tại: $PublishPath" -ForegroundColor Red
    exit 1
}

# Xóa file web.config cũ nếu có
if (Test-Path $webConfigPath) {
    Write-Host "Đang xóa file web.config cũ..." -ForegroundColor Yellow
    Remove-Item $webConfigPath -Force
    Write-Host "✓ Đã xóa file cũ" -ForegroundColor Green
}

# Nội dung web.config - đảm bảo không có ký tự đặc biệt
$webConfigContent = '<?xml version="1.0" encoding="utf-8"?>
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
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="' + $Environment + '" />
        </environmentVariables>
      </aspNetCore>
      <httpErrors errorMode="Detailed" />
    </system.webServer>
  </location>
</configuration>'

# Tạo file với UTF-8 without BOM
Write-Host "Đang tạo file web.config mới..." -ForegroundColor Cyan
try {
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($webConfigPath, $webConfigContent, $utf8NoBom)
    Write-Host "✓ Đã tạo file web.config thành công!" -ForegroundColor Green
} catch {
    Write-Host "✗ Lỗi khi tạo file: $_" -ForegroundColor Red
    exit 1
}

# Kiểm tra file đã được tạo
if (Test-Path $webConfigPath) {
    $fileInfo = Get-Item $webConfigPath
    Write-Host "`nThông tin file:" -ForegroundColor Cyan
    Write-Host "  Đường dẫn: $webConfigPath" -ForegroundColor White
    Write-Host "  Kích thước: $($fileInfo.Length) bytes" -ForegroundColor White
    Write-Host "  Encoding: UTF-8 (no BOM)" -ForegroundColor White
}

# Kiểm tra cú pháp XML
Write-Host "`nĐang kiểm tra cú pháp XML..." -ForegroundColor Cyan
try {
    $xml = New-Object System.Xml.XmlDocument
    $xml.Load($webConfigPath)
    Write-Host "✓ Cú pháp XML hợp lệ!" -ForegroundColor Green
    
    # Kiểm tra các thẻ quan trọng
    $handlers = $xml.SelectSingleNode("//handlers/add[@name='aspNetCore']")
    $aspNetCore = $xml.SelectSingleNode("//aspNetCore")
    
    if ($handlers -and $aspNetCore) {
        Write-Host "✓ Cấu hình đầy đủ!" -ForegroundColor Green
        Write-Host "  - Handler: aspNetCore" -ForegroundColor White
        Write-Host "  - Process: $($aspNetCore.GetAttribute('processPath'))" -ForegroundColor White
        Write-Host "  - Arguments: $($aspNetCore.GetAttribute('arguments'))" -ForegroundColor White
        Write-Host "  - Environment: $($aspNetCore.SelectSingleNode('environmentVariables/environmentVariable').GetAttribute('value'))" -ForegroundColor White
    } else {
        Write-Host "✗ Cấu hình thiếu thành phần!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Lỗi cú pháp XML: $_" -ForegroundColor Red
    Write-Host "  Dòng lỗi: $($_.Exception.LineNumber)" -ForegroundColor Red
    exit 1
}

# Kiểm tra encoding (không có BOM)
Write-Host "`nĐang kiểm tra encoding..." -ForegroundColor Cyan
$bytes = [System.IO.File]::ReadAllBytes($webConfigPath)
if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
    Write-Host "⚠ File có BOM, đang loại bỏ..." -ForegroundColor Yellow
    $content = [System.IO.File]::ReadAllText($webConfigPath, [System.Text.Encoding]::UTF8)
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($webConfigPath, $content, $utf8NoBom)
    Write-Host "✓ Đã loại bỏ BOM" -ForegroundColor Green
} else {
    Write-Host "✓ File không có BOM (đúng)" -ForegroundColor Green
}

Write-Host "`n=== Hoàn tất! ===" -ForegroundColor Green
Write-Host "File web.config đã được tạo tại: $webConfigPath" -ForegroundColor Cyan
Write-Host "`nBây giờ hãy:" -ForegroundColor Yellow
Write-Host "1. Mở IIS Manager" -ForegroundColor White
Write-Host "2. Right-click Application Pool 'quanlyfilesBE' → Recycle" -ForegroundColor White
Write-Host "3. Refresh website và kiểm tra lại" -ForegroundColor White

