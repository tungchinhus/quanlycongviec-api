# Script Build & Deploy Thủ Công - Từng Bước
# Hướng dẫn chi tiết: Xem BUILD_DEPLOY_MANUAL.md

param(
    [switch]$SkipBuild = $false,
    [switch]$SkipPublish = $false,
    [switch]$SkipDeploy = $false,
    [string]$PublishPath = ".\publish",
    [string]$ServerPath = "C:\inetpub\wwwroot\quanlyfilesBE",
    [string]$AppPoolName = "quanlyfilesBE",
    [string]$ServerAddress = "172.20.115.40"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BUILD & DEPLOY THỦ CÔNG - TỪNG BƯỚC  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Tìm thư mục project
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Get-ChildItem -Path $scriptDir -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1

if ($projectFile -eq $null) {
    $currentDir = Get-Location
    $projectFile = Get-ChildItem -Path $currentDir -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($projectFile -ne $null) {
        $scriptDir = $currentDir.Path
    }
}

if ($projectFile -eq $null) {
    Write-Host "ERROR: Không tìm thấy file .csproj!" -ForegroundColor Red
    exit 1
}

$ProjectPath = $projectFile.DirectoryName
Write-Host "✓ Project tìm thấy tại: $ProjectPath" -ForegroundColor Green
Write-Host ""

# Chuyển đến thư mục project
Push-Location $ProjectPath

# ========================================
# BƯỚC 1: Kiểm tra .NET SDK
# ========================================
Write-Host "[BƯỚC 1/7] Kiểm tra .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed"
    }
    Write-Host "  ✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "  ✗ ERROR: Không tìm thấy .NET SDK!" -ForegroundColor Red
    Write-Host "  Vui lòng cài đặt .NET 9.0 SDK từ: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
    Pop-Location
    exit 1
}
Write-Host ""

# ========================================
# BƯỚC 2: Clean (Tùy chọn)
# ========================================
if (-not $SkipBuild) {
    Write-Host "[BƯỚC 2/7] Clean build cũ..." -ForegroundColor Yellow
    dotnet clean -c Release | Out-Null
    Write-Host "  ✓ Clean hoàn tất" -ForegroundColor Green
    Write-Host ""
}

# ========================================
# BƯỚC 3: Restore Dependencies
# ========================================
if (-not $SkipBuild) {
    Write-Host "[BƯỚC 3/7] Restore dependencies..." -ForegroundColor Yellow
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ ERROR: Restore thất bại!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Write-Host "  ✓ Restore thành công" -ForegroundColor Green
    Write-Host ""
}

# ========================================
# BƯỚC 4: Build Application
# ========================================
if (-not $SkipBuild) {
    Write-Host "[BƯỚC 4/7] Build application (Release)..." -ForegroundColor Yellow
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ ERROR: Build thất bại!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Write-Host "  ✓ Build thành công" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[BƯỚC 4/7] Build - SKIPPED" -ForegroundColor Gray
    Write-Host ""
}

# ========================================
# BƯỚC 5: Publish Application
# ========================================
if (-not $SkipPublish) {
    Write-Host "[BƯỚC 5/7] Publish application..." -ForegroundColor Yellow
    
    $fullPublishPath = Join-Path $ProjectPath $PublishPath
    Write-Host "  Publish path: $fullPublishPath" -ForegroundColor Cyan
    
    # Xóa thư mục publish cũ nếu có
    if (Test-Path $fullPublishPath) {
        Write-Host "  Xóa thư mục publish cũ..." -ForegroundColor Gray
        Remove-Item $fullPublishPath -Recurse -Force
    }
    
    dotnet publish -c Release -o $fullPublishPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ ERROR: Publish thất bại!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    
    # Kiểm tra files đã publish
    $dllFile = Join-Path $fullPublishPath "quanlyfilesBE.dll"
    if (Test-Path $dllFile) {
        Write-Host "  ✓ Publish thành công" -ForegroundColor Green
        Write-Host "  ✓ File chính: quanlyfilesBE.dll" -ForegroundColor Green
        
        # Đếm số files
        $fileCount = (Get-ChildItem $fullPublishPath -File).Count
        Write-Host "  ✓ Tổng số files: $fileCount" -ForegroundColor Green
    } else {
        Write-Host "  ✗ WARNING: Không tìm thấy quanlyfilesBE.dll!" -ForegroundColor Yellow
    }
    Write-Host ""
} else {
    Write-Host "[BƯỚC 5/7] Publish - SKIPPED" -ForegroundColor Gray
    Write-Host ""
}

# ========================================
# BƯỚC 6: Deploy lên Server (Tùy chọn)
# ========================================
if (-not $SkipDeploy) {
    Write-Host "[BƯỚC 6/7] Deploy lên Server..." -ForegroundColor Yellow
    
    # Kiểm tra quyền Administrator
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if (-not $isAdmin) {
        Write-Host "  ⚠ WARNING: Không có quyền Administrator" -ForegroundColor Yellow
        Write-Host "  Bạn cần copy files thủ công hoặc chạy script với quyền Admin" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  Files đã publish tại: $fullPublishPath" -ForegroundColor Cyan
        Write-Host "  Copy lên server: $ServerPath" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  Hoặc dùng robocopy:" -ForegroundColor Cyan
        Write-Host "  robocopy `"$fullPublishPath`" `"$ServerPath`" /MIR /Z" -ForegroundColor White
    } else {
        # Tạo thư mục server nếu chưa có
        if (-not (Test-Path $ServerPath)) {
            Write-Host "  Tạo thư mục server: $ServerPath" -ForegroundColor Gray
            New-Item -ItemType Directory -Path $ServerPath -Force | Out-Null
        }
        
        # Copy files
        Write-Host "  Copy files lên server..." -ForegroundColor Gray
        $fullPublishPath = Join-Path $ProjectPath $PublishPath
        Copy-Item -Path "$fullPublishPath\*" -Destination $ServerPath -Recurse -Force
        
        Write-Host "  ✓ Copy thành công" -ForegroundColor Green
        
        # Restart IIS Application Pool
        Write-Host "  Restart IIS Application Pool..." -ForegroundColor Gray
        Import-Module WebAdministration -ErrorAction SilentlyContinue
        Restart-WebAppPool -Name $AppPoolName
        Start-Sleep -Seconds 2
        
        $poolState = Get-WebAppPoolState -Name $AppPoolName
        if ($poolState.Value -eq "Started") {
            Write-Host "  ✓ Application Pool: $($poolState.Value)" -ForegroundColor Green
        } else {
            Write-Host "  ✗ Application Pool: $($poolState.Value)" -ForegroundColor Red
        }
    }
    Write-Host ""
} else {
    Write-Host "[BƯỚC 6/7] Deploy - SKIPPED" -ForegroundColor Gray
    Write-Host ""
}

# ========================================
# BƯỚC 7: Test & Verification
# ========================================
Write-Host "[BƯỚC 7/7] Test & Verification..." -ForegroundColor Yellow

if (-not $SkipPublish) {
    $fullPublishPath = Join-Path $ProjectPath $PublishPath
    
    # Kiểm tra files quan trọng
    Write-Host "  Kiểm tra files quan trọng..." -ForegroundColor Gray
    
    $importantFiles = @(
        "quanlyfilesBE.dll",
        "web.config",
        "appsettings.json"
    )
    
    $allFilesExist = $true
    foreach ($file in $importantFiles) {
        $filePath = Join-Path $fullPublishPath $file
        if (Test-Path $filePath) {
            Write-Host "    ✓ $file" -ForegroundColor Green
        } else {
            Write-Host "    ✗ $file (MISSING)" -ForegroundColor Red
            $allFilesExist = $false
        }
    }
    
    if ($allFilesExist) {
        Write-Host "  ✓ Tất cả files quan trọng đã có" -ForegroundColor Green
    }
}

# Test API endpoint (nếu deploy)
if (-not $SkipDeploy) {
    Write-Host "  Test API endpoint..." -ForegroundColor Gray
    try {
        $response = Invoke-WebRequest -Uri "http://${ServerAddress}:8080/api/test-db" -Method GET -TimeoutSec 5 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-Host "  ✓ API đang hoạt động (Status: $($response.StatusCode))" -ForegroundColor Green
        }
    } catch {
        Write-Host "  ⚠ Không thể test API (có thể server chưa sẵn sàng)" -ForegroundColor Yellow
    }
}

Write-Host ""

# ========================================
# TÓM TẮT
# ========================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  HOÀN TẤT!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not $SkipPublish) {
    $fullPublishPath = Join-Path $ProjectPath $PublishPath
    Write-Host "✓ Files đã publish tại:" -ForegroundColor Green
    Write-Host "  $fullPublishPath" -ForegroundColor Cyan
    Write-Host ""
}

if (-not $SkipDeploy) {
    Write-Host "✓ CORS đã được cấu hình với:" -ForegroundColor Green
    Write-Host "  - http://localsite.thibidi.com" -ForegroundColor Cyan
    Write-Host "  - http://localhost:4200" -ForegroundColor Cyan
    Write-Host "  - Exposed Authorization header" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "✓ Test API:" -ForegroundColor Green
    Write-Host "  http://${ServerAddress}:8080/swagger" -ForegroundColor Cyan
    Write-Host "  http://${ServerAddress}:8080/api/test-db" -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "Xem hướng dẫn chi tiết: BUILD_DEPLOY_MANUAL.md" -ForegroundColor Yellow
Write-Host ""

Pop-Location







