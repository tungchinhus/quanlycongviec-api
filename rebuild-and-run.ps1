# Script để rebuild và chạy backend với logging chi tiết
# Dùng cho Development mode

param(
    [switch]$Watch = $false,
    [int]$Port = 5000
)

Write-Host "=== Rebuild and Run Backend ===" -ForegroundColor Green
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
Write-Host "`n[1/4] Checking .NET SDK..." -ForegroundColor Cyan
try {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed"
    }
    Write-Host ".NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Không tìm thấy .NET SDK!" -ForegroundColor Red
    Write-Host "Vui lòng cài đặt .NET SDK từ: https://aka.ms/dotnet/download" -ForegroundColor Yellow
    Pop-Location
    exit 1
}

# Clean build
Write-Host "`n[2/4] Cleaning previous build..." -ForegroundColor Cyan
dotnet clean
if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: Clean có thể đã thất bại, nhưng tiếp tục build..." -ForegroundColor Yellow
}

# Build project
Write-Host "`n[3/4] Building project..." -ForegroundColor Cyan
dotnet build --configuration Debug --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build thất bại!" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "Build thành công!" -ForegroundColor Green

# Set environment
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:$Port"

# Run application
Write-Host "`n[4/4] Starting application..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Green
Write-Host "Backend API: http://localhost:$Port" -ForegroundColor Yellow
Write-Host "Swagger UI: http://localhost:$Port/swagger" -ForegroundColor Yellow
Write-Host "Test endpoint: http://localhost:$Port/api/work-items/test" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Green
Write-Host "`nLogs sẽ hiển thị ở đây. Nhấn Ctrl+C để dừng.`n" -ForegroundColor Cyan

if ($Watch) {
    Write-Host "Running with watch mode (auto-reload on file changes)..." -ForegroundColor Yellow
    dotnet watch run --launch-profile http
} else {
    dotnet run --launch-profile http
}

Pop-Location


