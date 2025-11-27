# Script Deploy .NET 9.0 Application lên IIS
# Chạy với quyền Administrator

param(
    [string]$PublishPath = "C:\inetpub\wwwroot\quanlyfilesBE",
    [string]$AppPoolName = "quanlyfilesBE",
    [string]$SiteName = "quanlyfilesBE",
    [int]$Port = 80,
    [string]$Environment = "Production"
)

Write-Host "=== Deploy quanlyfilesBE to IIS ===" -ForegroundColor Green

# Kiểm tra quyền Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: Script phải chạy với quyền Administrator!" -ForegroundColor Red
    exit 1
}

# Bước 1: Publish Application
Write-Host "[1/7] Publishing application..." -ForegroundColor Yellow
$projectPath = Join-Path $PSScriptRoot "quanlyfilesBE.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Host "ERROR: Không tìm thấy file .csproj tại $projectPath" -ForegroundColor Red
    exit 1
}

# Tạo thư mục publish nếu chưa có
if (-not (Test-Path $PublishPath)) {
    New-Item -ItemType Directory -Path $PublishPath -Force | Out-Null
    Write-Host "Đã tạo thư mục: $PublishPath" -ForegroundColor Green
}

# Publish
Write-Host "Đang publish với Release configuration..." -ForegroundColor Cyan
dotnet publish $projectPath -c Release -o $PublishPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Publish thất bại!" -ForegroundColor Red
    exit 1
}

Write-Host "Publish thành công!" -ForegroundColor Green

# Bước 2: Tạo web.config nếu chưa có
Write-Host "[2/7] Kiểm tra web.config..." -ForegroundColor Yellow
$webConfigPath = Join-Path $PublishPath "web.config"

if (-not (Test-Path $webConfigPath)) {
    Write-Host "Tạo file web.config..." -ForegroundColor Cyan
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
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="$Environment" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@
    Set-Content -Path $webConfigPath -Value $webConfigContent -Encoding UTF8
    Write-Host "Đã tạo web.config" -ForegroundColor Green
} else {
    Write-Host "web.config đã tồn tại" -ForegroundColor Green
}

# Bước 3: Tạo thư mục logs
Write-Host "[3/7] Tạo thư mục logs..." -ForegroundColor Yellow
$logsPath = Join-Path $PublishPath "logs"
if (-not (Test-Path $logsPath)) {
    New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
    Write-Host "Đã tạo thư mục logs" -ForegroundColor Green
}

# Bước 4: Import IIS Module
Write-Host "[4/7] Kiểm tra IIS Module..." -ForegroundColor Yellow
Import-Module WebAdministration -ErrorAction SilentlyContinue

if (-not (Get-Module WebAdministration)) {
    Write-Host "ERROR: Không thể import WebAdministration module!" -ForegroundColor Red
    Write-Host "Hãy cài đặt IIS Management Tools" -ForegroundColor Yellow
    exit 1
}

# Bước 5: Tạo Application Pool
Write-Host "`n[5/7] Tạo/Update Application Pool..." -ForegroundColor Yellow
$appPool = Get-WebAppPoolState -Name $AppPoolName -ErrorAction SilentlyContinue

if (-not $appPool) {
    Write-Host "Tạo Application Pool: $AppPoolName" -ForegroundColor Cyan
    New-WebAppPool -Name $AppPoolName
    Write-Host "Đã tạo Application Pool" -ForegroundColor Green
} else {
    Write-Host "Application Pool đã tồn tại" -ForegroundColor Green
}

# Cấu hình Application Pool
Write-Host "Cấu hình Application Pool..." -ForegroundColor Cyan
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name managedPipelineMode -Value "Integrated"
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name startMode -Value "AlwaysRunning"
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name processModel.idleTimeout -Value ([TimeSpan]::FromMinutes(0))
Write-Host "Đã cấu hình Application Pool" -ForegroundColor Green

# Bước 6: Tạo Website
Write-Host "[6/7] Tạo/Update Website..." -ForegroundColor Yellow
$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue

if (-not $site) {
    Write-Host "Tạo Website: $SiteName" -ForegroundColor Cyan
    New-Website -Name $SiteName `
                -Port $Port `
                -PhysicalPath $PublishPath `
                -ApplicationPool $AppPoolName
    Write-Host "Đã tạo Website" -ForegroundColor Green
} else {
    Write-Host "Website đã tồn tại, cập nhật cấu hình..." -ForegroundColor Cyan
    Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name physicalPath -Value $PublishPath
    Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name applicationPool -Value $AppPoolName
    Write-Host "Đã cập nhật Website" -ForegroundColor Green
}

# Bước 7: Cấp quyền
Write-Host "`n[7/7] Cấp quyền truy cập..." -ForegroundColor Yellow

# Cấp quyền cho Application Pool Identity
$appPoolIdentity = "IIS AppPool\$AppPoolName"
Write-Host "Cấp quyền cho $appPoolIdentity..." -ForegroundColor Cyan

# Quyền đọc cho thư mục ứng dụng
icacls $PublishPath /grant "${appPoolIdentity}:(OI)(CI)(RX)" /T /Q | Out-Null

# Quyền ghi cho thư mục logs
icacls $logsPath /grant "${appPoolIdentity}:(OI)(CI)(F)" /T /Q | Out-Null

# Quyền cho service-account-key.json nếu có
$serviceAccountKey = Join-Path $PublishPath "service-account-key.json"
if (Test-Path $serviceAccountKey) {
    icacls $serviceAccountKey /grant "${appPoolIdentity}:(R)" /Q | Out-Null
}

Write-Host "Đã cấp quyền" -ForegroundColor Green

# Khởi động Application Pool
Write-Host 'Khoi dong Application Pool...' -ForegroundColor Yellow
Restart-WebAppPool -Name $AppPoolName
Start-Sleep -Seconds 2

# Kiểm tra trạng thái
$poolState = Get-WebAppPoolState -Name $AppPoolName
Write-Host "Application Pool State: $($poolState.Value)" -ForegroundColor $(if ($poolState.Value -eq "Started") { "Green" } else { "Red" })

# Thông tin kết quả
Write-Host "=== Deploy hoàn tất! ===" -ForegroundColor Green
Write-Host "Website URL: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "Swagger URL: http://localhost:$Port/swagger" -ForegroundColor Cyan
Write-Host "Physical Path: $PublishPath" -ForegroundColor Cyan
Write-Host "Lưu ý:" -ForegroundColor Yellow
Write-Host "1. Kiểm tra appsettings.json và cập nhật connection string" -ForegroundColor Yellow
Write-Host "2. Kiểm tra quyền truy cập database" -ForegroundColor Yellow
Write-Host "3. Kiểm tra logs tại: $logsPath" -ForegroundColor Yellow

