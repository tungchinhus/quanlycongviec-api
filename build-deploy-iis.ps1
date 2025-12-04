# ============================================
# Script Build và Deploy lên IIS
# QuanLyFiles Backend - .NET 9.0
# ============================================
# Cách sử dụng:
#   1. Chạy với quyền Administrator
#   2. .\build-deploy-iis.ps1
#   3. Hoặc với tham số: .\build-deploy-iis.ps1 -PublishPath "C:\inetpub\wwwroot\quanlyfilesBE" -Port 8080
# ============================================

param(
    [string]$PublishPath = "C:\inetpub\wwwroot\quanlyfilesBE",
    [string]$AppPoolName = "quanlyfilesBE",
    [string]$SiteName = "quanlyfilesBE",
    [int]$Port = 8080,
    [string]$Environment = "Production",
    [switch]$SkipBuild = $false,
    [switch]$SkipDeploy = $false
)

$ErrorActionPreference = "Stop"

# Màu sắc cho output
function Write-Step {
    param([string]$Message, [int]$Step, [int]$Total)
    Write-Host "`n[$Step/$Total] $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "  → $Message" -ForegroundColor Cyan
}

# Header
Write-Host "`n============================================" -ForegroundColor Green
Write-Host "  BUILD & DEPLOY TO IIS" -ForegroundColor Green
Write-Host "  QuanLyFiles Backend - .NET 9.0" -ForegroundColor Green
Write-Host "============================================`n" -ForegroundColor Green

# Kiểm tra quyền Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Script phải chạy với quyền Administrator!"
    Write-Info "Hãy right-click PowerShell và chọn 'Run as Administrator'"
    exit 1
}

# Tìm project file
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Get-ChildItem -Path $scriptDir -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1

if ($null -eq $projectFile) {
    $currentDir = Get-Location
    $projectFile = Get-ChildItem -Path $currentDir -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $projectFile) {
        $scriptDir = $currentDir.Path
    }
}

if ($null -eq $projectFile) {
    Write-Error "Không tìm thấy file .csproj!"
    exit 1
}

$ProjectPath = $projectFile.DirectoryName
$ProjectFile = $projectFile.FullName
Write-Info "Project: $ProjectFile"

# Kiểm tra .NET SDK
Write-Step "Kiểm tra .NET SDK" 1 8
try {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed"
    }
    Write-Success ".NET SDK version: $dotnetVersion"
} catch {
    Write-Error ".NET SDK không được tìm thấy!"
    Write-Info "Tải về từ: https://dotnet.microsoft.com/download/dotnet/9.0"
    Write-Info "Chọn 'SDK' (không phải Runtime)"
    exit 1
}

# Chuyển đến thư mục project
Push-Location $ProjectPath

try {
    # Bước 2: Clean (tùy chọn)
    if (-not $SkipBuild) {
        Write-Step "Clean project" 2 8
        dotnet clean -c Release 2>&1 | Out-Null
        Write-Success "Clean hoàn tất"
    }

    # Bước 3: Restore dependencies
    if (-not $SkipBuild) {
        Write-Step "Restore dependencies" 3 8
        dotnet restore
        if ($LASTEXITCODE -ne 0) {
            throw "Restore thất bại!"
        }
        Write-Success "Restore hoàn tất"
    }

    # Bước 4: Build
    if (-not $SkipBuild) {
        Write-Step "Build application (Release)" 4 8
        dotnet build -c Release --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "Build thất bại!"
        }
        Write-Success "Build thành công"
    }

    # Bước 5: Publish
    if (-not $SkipDeploy) {
        Write-Step "Publish application" 5 8
        Write-Info "Publish path: $PublishPath"
        
        # Tạo thư mục publish nếu chưa có
        if (-not (Test-Path $PublishPath)) {
            New-Item -ItemType Directory -Path $PublishPath -Force | Out-Null
            Write-Info "Đã tạo thư mục: $PublishPath"
        }

        # Backup nếu thư mục đã có file
        $backupPath = "$PublishPath.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        if ((Test-Path $PublishPath) -and (Get-ChildItem $PublishPath -ErrorAction SilentlyContinue)) {
            Write-Info "Tạo backup: $backupPath"
            Copy-Item -Path $PublishPath -Destination $backupPath -Recurse -Force -ErrorAction SilentlyContinue
        }

        # Publish
        dotnet publish -c Release -o $PublishPath --no-build --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "Publish thất bại!"
        }
        Write-Success "Publish thành công"
    }

    # Bước 6: Cấu hình web.config
    if (-not $SkipDeploy) {
        Write-Step "Kiểm tra web.config" 6 8
        $webConfigPath = Join-Path $PublishPath "web.config"
        
        if (-not (Test-Path $webConfigPath)) {
            Write-Info "Tạo file web.config..."
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
            Set-Content -Path $webConfigPath -Value $webConfigContent -Encoding UTF8
            Write-Success "Đã tạo web.config"
        } else {
            Write-Success "web.config đã tồn tại"
        }
    }

    # Bước 7: Tạo thư mục logs
    if (-not $SkipDeploy) {
        Write-Step "Tạo thư mục logs" 7 8
        $logsPath = Join-Path $PublishPath "logs"
        if (-not (Test-Path $logsPath)) {
            New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
            Write-Success "Đã tạo thư mục logs"
        } else {
            Write-Success "Thư mục logs đã tồn tại"
        }
    }

    # Bước 8: Cấu hình IIS
    if (-not $SkipDeploy) {
        Write-Step "Cấu hình IIS" 8 8
        
        # Import IIS Module
        Import-Module WebAdministration -ErrorAction SilentlyContinue
        if (-not (Get-Module WebAdministration)) {
            Write-Error "Không thể import WebAdministration module!"
            Write-Info "Hãy cài đặt IIS Management Tools"
            exit 1
        }

        # Tạo Application Pool
        $appPool = Get-WebAppPoolState -Name $AppPoolName -ErrorAction SilentlyContinue
        if (-not $appPool) {
            Write-Info "Tạo Application Pool: $AppPoolName"
            New-WebAppPool -Name $AppPoolName
            Write-Success "Đã tạo Application Pool"
        } else {
            Write-Success "Application Pool đã tồn tại"
        }

        # Cấu hình Application Pool
        Write-Info "Cấu hình Application Pool..."
        Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
        Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name managedPipelineMode -Value "Integrated"
        Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name startMode -Value "AlwaysRunning"
        Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name processModel.idleTimeout -Value ([TimeSpan]::FromMinutes(0))
        Write-Success "Đã cấu hình Application Pool"

        # Tạo/Update Website
        $site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
        if (-not $site) {
            Write-Info "Tạo Website: $SiteName"
            New-Website -Name $SiteName `
                        -Port $Port `
                        -PhysicalPath $PublishPath `
                        -ApplicationPool $AppPoolName
            Write-Success "Đã tạo Website"
        } else {
            Write-Info "Website đã tồn tại, cập nhật cấu hình..."
            Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name physicalPath -Value $PublishPath
            Set-ItemProperty -Path "IIS:\Sites\$SiteName" -Name applicationPool -Value $AppPoolName
            
            # Cập nhật binding nếu port thay đổi
            $binding = Get-WebBinding -Name $SiteName | Where-Object { $_.protocol -eq "http" } | Select-Object -First 1
            if ($null -ne $binding -and $binding.bindingInformation -notlike "*:${Port}:*") {
                Remove-WebBinding -Name $SiteName -Protocol http -BindingInformation $binding.bindingInformation
                New-WebBinding -Name $SiteName -Protocol http -Port $Port
                Write-Info "Đã cập nhật port thành $Port"
            }
            Write-Success "Đã cập nhật Website"
        }

        # Cấp quyền
        Write-Info "Cấp quyền truy cập..."
        $appPoolIdentity = "IIS AppPool\$AppPoolName"
        
        # Quyền đọc cho thư mục ứng dụng
        icacls $PublishPath /grant "${appPoolIdentity}:(OI)(CI)(RX)" /T /Q | Out-Null
        
        # Quyền ghi cho thư mục logs
        $logsPath = Join-Path $PublishPath "logs"
        icacls $logsPath /grant "${appPoolIdentity}:(OI)(CI)(F)" /T /Q | Out-Null
        
        # Quyền cho service-account-key.json nếu có
        $serviceAccountKey = Join-Path $PublishPath "service-account-key.json"
        if (Test-Path $serviceAccountKey) {
            icacls $serviceAccountKey /grant "${appPoolIdentity}:(R)" /Q | Out-Null
        }
        
        # Quyền cho thư mục file storage nếu có
        $storagePath = "C:\THIBIDI-STORE\p-TK"
        if (Test-Path $storagePath) {
            icacls $storagePath /grant "${appPoolIdentity}:(OI)(CI)(F)" /T /Q | Out-Null
            Write-Info "Đã cấp quyền cho file storage"
        }
        
        Write-Success "Đã cấp quyền"

        # Restart Application Pool
        Write-Info "Khởi động lại Application Pool..."
        Restart-WebAppPool -Name $AppPoolName
        Start-Sleep -Seconds 3

        # Kiểm tra trạng thái
        $poolState = Get-WebAppPoolState -Name $AppPoolName
        if ($poolState.Value -eq "Started") {
            Write-Success "Application Pool đang chạy: $($poolState.Value)"
        } else {
            Write-Error "Application Pool không khởi động được: $($poolState.Value)"
        }
    }

    # Kết quả
    Write-Host "`n============================================" -ForegroundColor Green
    Write-Host "  DEPLOY HOÀN TẤT!" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Thông tin:" -ForegroundColor Cyan
    Write-Host "  Website URL:     http://localhost:$Port" -ForegroundColor White
    Write-Host "  Swagger URL:     http://localhost:$Port/swagger" -ForegroundColor White
    Write-Host "  API Base URL:    http://localhost:$Port/api" -ForegroundColor White
    Write-Host "  Physical Path:   $PublishPath" -ForegroundColor White
    Write-Host "  Application Pool: $AppPoolName" -ForegroundColor White
    Write-Host ""
    Write-Host "Lưu ý quan trọng:" -ForegroundColor Yellow
    Write-Host "  1. Kiểm tra appsettings.json và cập nhật connection string" -ForegroundColor Yellow
    Write-Host "  2. Kiểm tra quyền truy cập database" -ForegroundColor Yellow
    Write-Host "  3. Kiểm tra logs tại: $PublishPath\logs" -ForegroundColor Yellow
    Write-Host "  4. Test API: http://localhost:$Port/api/test-db" -ForegroundColor Yellow
    Write-Host ""

} catch {
    Write-Error "Lỗi: $($_.Exception.Message)"
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}

