# Script để build trên máy Dev và chuẩn bị files để copy lên Server
# Chạy trên máy Dev (cần .NET SDK)

param(
    [string]$OutputPath = ".\publish",
    [string]$Configuration = "Release"
)

Write-Host "=== Build for Server Deployment ===" -ForegroundColor Green

# Check .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Cyan
$dotnetCheck = dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: .NET SDK not found!" -ForegroundColor Red
    Write-Host "Please install .NET SDK from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}
Write-Host ".NET SDK version: $dotnetCheck" -ForegroundColor Green

# Get project directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Get-ChildItem -Path $scriptDir -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1

if ($projectFile -eq $null) {
    Write-Host "ERROR: Cannot find .csproj file!" -ForegroundColor Red
    exit 1
}

$ProjectPath = $projectFile.DirectoryName
Write-Host "Project found at: $ProjectPath" -ForegroundColor Cyan

# Change to project directory
Push-Location $ProjectPath

# Clean previous build
Write-Host "Cleaning previous build..." -ForegroundColor Cyan
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Recurse -Force
}

# Build
Write-Host "[1/2] Building application..." -ForegroundColor Cyan
dotnet build -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}

# Publish
Write-Host "[2/2] Publishing application..." -ForegroundColor Cyan
$fullOutputPath = Join-Path $ProjectPath $OutputPath
dotnet publish -c $Configuration -o $fullOutputPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Publish failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location

# Show summary
Write-Host ""
Write-Host "=== Build Complete! ===" -ForegroundColor Green
Write-Host "Published files location: $fullOutputPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Copy entire folder '$OutputPath' to server" -ForegroundColor White
Write-Host "2. Server location: C:\inetpub\wwwroot\quanlyfilesBE" -ForegroundColor White
Write-Host "3. On server, run: .\restart-iis-only.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Files ready to deploy:" -ForegroundColor Cyan
Get-ChildItem $fullOutputPath -File | Select-Object -First 10 Name, Length | Format-Table

