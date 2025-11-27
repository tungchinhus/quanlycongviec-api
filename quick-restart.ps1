# Script nhanh để build và restart application
# Dùng cho cả Development và Production
# Compatible with PowerShell 5.1+

param(
    [switch]$DeployToIIS = $false,
    [string]$PublishPath = "C:\inetpub\wwwroot\quanlyfilesBE",
    [string]$AppPoolName = "quanlyfilesBE",
    [string]$ProjectPath = ""
)

Write-Host "=== Quick Restart Application ===" -ForegroundColor Green

# Tìm thư mục project (nơi có file .csproj)
if ([string]::IsNullOrEmpty($ProjectPath)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $projectFile = Get-ChildItem -Path $scriptDir -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1
    
    if ($projectFile -ne $null) {
        $ProjectPath = $projectFile.DirectoryName
    }
    
    if ([string]::IsNullOrEmpty($ProjectPath)) {
        $currentDir = Get-Location
        $projectFile = Get-ChildItem -Path $currentDir -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1
        
        if ($projectFile -ne $null) {
            $ProjectPath = $currentDir.Path
        }
    }
    
    if ([string]::IsNullOrEmpty($ProjectPath)) {
        Write-Host "ERROR: Không tìm thấy file .csproj!" -ForegroundColor Red
        Write-Host "Hãy chạy script từ thư mục project hoặc chỉ định -ProjectPath" -ForegroundColor Yellow
        exit 1
    }
}

if ($ProjectPath -ne $null -and $ProjectPath -ne "") {
    Write-Host "Tìm thấy project tại: $ProjectPath" -ForegroundColor Cyan
    if (-not (Test-Path $ProjectPath)) {
        Write-Host "ERROR: Đường dẫn project không tồn tại: $ProjectPath" -ForegroundColor Red
        exit 1
    }
}

# Kiểm tra .NET SDK
Write-Host "Kiểm tra .NET SDK..." -ForegroundColor Cyan
try {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed"
    }
    Write-Host ".NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Khong tim thay .NET SDK!" -ForegroundColor Red
    Write-Host "Vui long cai dat .NET SDK tu: https://aka.ms/dotnet/download" -ForegroundColor Yellow
    exit 1
}

# Chuyển đến thư mục project
Push-Location $ProjectPath

# Kiểm tra xem đang chạy development hay production
$isDevelopment = $env:ASPNETCORE_ENVIRONMENT -eq "Development"

if ($DeployToIIS -or -not $isDevelopment) {
    Write-Host "[Production Mode] Build và Deploy lên IIS..." -ForegroundColor Yellow
    
    # Kiểm tra quyền Administrator
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Host "ERROR: Script phải chạy với quyền Administrator để deploy lên IIS!" -ForegroundColor Red
        Write-Host "Chạy lại PowerShell với quyền Administrator" -ForegroundColor Yellow
        Pop-Location
        exit 1
    }
    
    # Build và Publish
    Write-Host "[1/3] Building application..." -ForegroundColor Cyan
    dotnet build -c Release
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build thất bại!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    
    Write-Host "[2/3] Publishing application..." -ForegroundColor Cyan
    dotnet publish -c Release -o $PublishPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Publish thất bại!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    
    Write-Host "[3/3] Restarting IIS Application Pool..." -ForegroundColor Cyan
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    Restart-WebAppPool -Name $AppPoolName
    Start-Sleep -Seconds 2
    
    $poolState = Get-WebAppPoolState -Name $AppPoolName
    if ($poolState.Value -eq "Started") {
        Write-Host "Application Pool State: $($poolState.Value)" -ForegroundColor Green
    }
    else {
        Write-Host "Application Pool State: $($poolState.Value)" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "=== Deploy hoàn tất! ===" -ForegroundColor Green
    Write-Host "CORS đã được cập nhật với:" -ForegroundColor Cyan
    Write-Host "  - http://localsite.thibidi.com" -ForegroundColor Cyan
    Write-Host "  - http://localhost:4200" -ForegroundColor Cyan
    Write-Host "  - Exposed Authorization header" -ForegroundColor Cyan
    
    Pop-Location
}
else {
    Write-Host "[Development Mode] Chỉ cần restart..." -ForegroundColor Yellow
    Write-Host "Nếu đang chạy dotnet run, nhấn Ctrl+C để dừng và chạy lại:" -ForegroundColor Cyan
    Write-Host "  dotnet run" -ForegroundColor White
    Write-Host ""
    Write-Host "Hoặc nếu muốn build lại:" -ForegroundColor Cyan
    Write-Host "  dotnet build" -ForegroundColor White
    Write-Host "  dotnet run" -ForegroundColor White
    
    Pop-Location
}
