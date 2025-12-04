# ============================================
# Script Publish với dừng IIS tạm thời
# Giải quyết lỗi "Access denied" khi publish
# ============================================
# Cách sử dụng:
#   1. Chạy với quyền Administrator
#   2. .\publish-with-iis-stop.ps1
# ============================================

param(
    [string]$PublishPath = "C:\inetpub\wwwroot\quanlyfilesBE",
    [string]$AppPoolName = "quanlyfilesBE",
    [string]$SiteName = "quanlyfilesBE"
)

$ErrorActionPreference = "Stop"

Write-Host "`n============================================" -ForegroundColor Green
Write-Host "  PUBLISH WITH IIS STOP" -ForegroundColor Green
Write-Host "============================================`n" -ForegroundColor Green

# Kiểm tra quyền Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: Script phải chạy với quyền Administrator!" -ForegroundColor Red
    Write-Host "Hãy right-click PowerShell và chọn 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Import IIS Module
Import-Module WebAdministration -ErrorAction SilentlyContinue
if (-not (Get-Module WebAdministration)) {
    Write-Host "ERROR: Không thể import WebAdministration module!" -ForegroundColor Red
    Write-Host "Hãy cài đặt IIS Management Tools" -ForegroundColor Yellow
    exit 1
}

try {
    # Bước 1: Dừng Application Pool và Website
    Write-Host "[1/4] Dừng Application Pool và Website..." -ForegroundColor Yellow
    
    $appPool = Get-WebAppPoolState -Name $AppPoolName -ErrorAction SilentlyContinue
    if ($appPool) {
        if ($appPool.Value -eq "Started") {
            Write-Host "  → Dừng Application Pool: $AppPoolName" -ForegroundColor Cyan
            Stop-WebAppPool -Name $AppPoolName
            Start-Sleep -Seconds 2
            Write-Host "  ✓ Đã dừng Application Pool" -ForegroundColor Green
        } else {
            Write-Host "  ✓ Application Pool đã dừng" -ForegroundColor Green
        }
    } else {
        Write-Host "  → Application Pool không tồn tại, bỏ qua" -ForegroundColor Yellow
    }

    $site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
    if ($site) {
        $siteState = (Get-WebsiteState -Name $SiteName).Value
        if ($siteState -eq "Started") {
            Write-Host "  → Dừng Website: $SiteName" -ForegroundColor Cyan
            Stop-Website -Name $SiteName
            Start-Sleep -Seconds 1
            Write-Host "  ✓ Đã dừng Website" -ForegroundColor Green
        } else {
            Write-Host "  ✓ Website đã dừng" -ForegroundColor Green
        }
    } else {
        Write-Host "  → Website không tồn tại, bỏ qua" -ForegroundColor Yellow
    }

    # Bước 2: Tạo thư mục publish nếu chưa có
    Write-Host "`n[2/4] Kiểm tra thư mục publish..." -ForegroundColor Yellow
    if (-not (Test-Path $PublishPath)) {
        Write-Host "  → Tạo thư mục: $PublishPath" -ForegroundColor Cyan
        New-Item -ItemType Directory -Path $PublishPath -Force | Out-Null
        Write-Host "  ✓ Đã tạo thư mục" -ForegroundColor Green
    } else {
        Write-Host "  ✓ Thư mục đã tồn tại" -ForegroundColor Green
    }

    # Bước 3: Publish
    Write-Host "`n[3/4] Publish application..." -ForegroundColor Yellow
    Write-Host "  → Publish path: $PublishPath" -ForegroundColor Cyan
    
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
        Write-Host "ERROR: Không tìm thấy file .csproj!" -ForegroundColor Red
        exit 1
    }

    $ProjectPath = $projectFile.DirectoryName
    Push-Location $ProjectPath

    Write-Host "  → Project: $($projectFile.Name)" -ForegroundColor Cyan
    dotnet publish -c Release -o $PublishPath

    if ($LASTEXITCODE -ne 0) {
        throw "Publish thất bại!"
    }

    Write-Host "  ✓ Publish thành công" -ForegroundColor Green
    Pop-Location

    # Bước 4: Khởi động lại Application Pool và Website
    Write-Host "`n[4/4] Khởi động lại Application Pool và Website..." -ForegroundColor Yellow
    
    if ($appPool) {
        Write-Host "  → Khởi động Application Pool: $AppPoolName" -ForegroundColor Cyan
        Start-WebAppPool -Name $AppPoolName
        Start-Sleep -Seconds 2
        
        $poolState = Get-WebAppPoolState -Name $AppPoolName
        if ($poolState.Value -eq "Started") {
            Write-Host "  ✓ Application Pool đang chạy" -ForegroundColor Green
        } else {
            Write-Host "  ✗ Application Pool không khởi động được: $($poolState.Value)" -ForegroundColor Red
        }
    }

    if ($site) {
        Write-Host "  → Khởi động Website: $SiteName" -ForegroundColor Cyan
        Start-Website -Name $SiteName
        Start-Sleep -Seconds 1
        
        $siteState = (Get-WebsiteState -Name $SiteName).Value
        if ($siteState -eq "Started") {
            Write-Host "  ✓ Website đang chạy" -ForegroundColor Green
        } else {
            Write-Host "  ✗ Website không khởi động được: $siteState" -ForegroundColor Red
        }
    }

    # Kết quả
    Write-Host "`n============================================" -ForegroundColor Green
    Write-Host "  PUBLISH HOÀN TẤT!" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Thông tin:" -ForegroundColor Cyan
    Write-Host "  Publish Path: $PublishPath" -ForegroundColor White
    Write-Host ""

} catch {
    Write-Host "`nERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    
    # Cố gắng khởi động lại IIS nếu có lỗi
    Write-Host "`nĐang cố gắng khởi động lại IIS..." -ForegroundColor Yellow
    try {
        if ($appPool) {
            Start-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
        }
        if ($site) {
            Start-Website -Name $SiteName -ErrorAction SilentlyContinue
        }
    } catch {
        Write-Host "Không thể khởi động lại IIS tự động" -ForegroundColor Yellow
    }
    
    exit 1
}

