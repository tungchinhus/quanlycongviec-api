param(
    [switch]$DeployToIIS = $false,
    [string]$PublishPath = "C:\inetpub\wwwroot\quanlyfilesBE",
    [string]$AppPoolName = "quanlyfilesBE"
)

Write-Host "=== Build and Deploy ===" -ForegroundColor Green

# Find project directory
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
    Write-Host "ERROR: Cannot find .csproj file!" -ForegroundColor Red
    exit 1
}

$ProjectPath = $projectFile.DirectoryName
Write-Host "Project found at: $ProjectPath" -ForegroundColor Cyan

# Check .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Cyan
$dotnetCheck = dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: .NET SDK not found!" -ForegroundColor Red
    Write-Host "Please install .NET SDK from: https://aka.ms/dotnet/download" -ForegroundColor Yellow
    exit 1
}
Write-Host ".NET SDK version: $dotnetCheck" -ForegroundColor Green

# Change to project directory
Push-Location $ProjectPath

if ($DeployToIIS) {
    Write-Host "[Production Mode] Building and deploying to IIS..." -ForegroundColor Yellow
    
    # Check Administrator rights
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Host "ERROR: Script must run as Administrator!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    
    # Build
    Write-Host "[1/3] Building application..." -ForegroundColor Cyan
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    
    # Publish
    Write-Host "[2/3] Publishing application..." -ForegroundColor Cyan
    dotnet publish -c Release -o $PublishPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Publish failed!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    
    # Restart IIS
    Write-Host "[3/3] Restarting IIS Application Pool..." -ForegroundColor Cyan
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    Restart-WebAppPool -Name $AppPoolName
    Start-Sleep -Seconds 2
    
    $poolState = Get-WebAppPoolState -Name $AppPoolName
    if ($poolState.Value -eq "Started") {
        Write-Host "Application Pool State: $($poolState.Value)" -ForegroundColor Green
    } else {
        Write-Host "Application Pool State: $($poolState.Value)" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "=== Deploy Complete! ===" -ForegroundColor Green
    Write-Host "CORS updated with:" -ForegroundColor Cyan
    Write-Host "  - http://localsite.thibidi.com" -ForegroundColor Cyan
    Write-Host "  - http://localhost:4200" -ForegroundColor Cyan
    Write-Host "  - Exposed Authorization header" -ForegroundColor Cyan
    
    Pop-Location
} else {
    Write-Host "[Development Mode] Just restart needed..." -ForegroundColor Yellow
    Pop-Location
}

