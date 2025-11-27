# Script để chỉ rebuild backend (không chạy)
# Dùng khi muốn build nhanh để test

Write-Host "=== Rebuild Backend Only ===" -ForegroundColor Green
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
Write-Host "Project found at: $ProjectPath" -ForegroundColor Cyan

# Chuyển đến thư mục project
Push-Location $ProjectPath

# Kiểm tra .NET SDK
Write-Host "`nChecking .NET SDK..." -ForegroundColor Cyan
try {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed"
    }
    Write-Host ".NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Không tìm thấy .NET SDK!" -ForegroundColor Red
    Pop-Location
    exit 1
}

# Clean và build
Write-Host "`nCleaning..." -ForegroundColor Cyan
dotnet clean | Out-Null

Write-Host "Building..." -ForegroundColor Cyan
dotnet build --configuration Debug

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n=== Build thành công! ===" -ForegroundColor Green
    Write-Host "Bây giờ bạn có thể chạy: dotnet run" -ForegroundColor Yellow
    Write-Host "Hoặc: .\rebuild-and-run.ps1" -ForegroundColor Yellow
} else {
    Write-Host "`n=== Build thất bại! ===" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location


