# Script to fix PersonConfirmation column type mismatch
# Converts PersonConfirmation from nvarchar(50) to bit in SQL Server database

param(
    [string]$ConnectionString = "",
    [switch]$SkipBackup = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Fix PersonConfirmation Migration Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get connection string from appsettings.json if not provided
if ([string]::IsNullOrEmpty($ConnectionString)) {
    Write-Host "Reading connection string from appsettings.json..." -ForegroundColor Yellow
    
    $appsettingsPath = Join-Path $PSScriptRoot "appsettings.json"
    if (-not (Test-Path $appsettingsPath)) {
        Write-Host "ERROR: appsettings.json not found!" -ForegroundColor Red
        exit 1
    }
    
    $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
    $ConnectionString = $appsettings.ConnectionStrings.DefaultConnection
    
    if ([string]::IsNullOrEmpty($ConnectionString)) {
        Write-Host "ERROR: Connection string not found in appsettings.json!" -ForegroundColor Red
        exit 1
    }
}

# Parse connection string
$server = ""
$database = ""
$userId = ""
$password = ""

if ($ConnectionString -match "Server=([^;]+)") {
    $server = $matches[1]
}
if ($ConnectionString -match "Database=([^;]+)") {
    $database = $matches[1]
}
if ($ConnectionString -match "User Id=([^;]+)") {
    $userId = $matches[1]
}
if ($ConnectionString -match "Password=([^;]+)") {
    $password = $matches[1]
}

Write-Host "Connection Details:" -ForegroundColor Green
Write-Host "  Server: $server" -ForegroundColor Gray
Write-Host "  Database: $database" -ForegroundColor Gray
Write-Host "  User: $userId" -ForegroundColor Gray
Write-Host ""

# Check if SQL Server module is available
$sqlModule = Get-Module -ListAvailable -Name SqlServer
if (-not $sqlModule) {
    Write-Host "Installing SqlServer PowerShell module..." -ForegroundColor Yellow
    Install-Module -Name SqlServer -Scope CurrentUser -Force -AllowClobber
    Import-Module SqlServer
}

# Backup database if not skipped
if (-not $SkipBackup) {
    Write-Host "Creating database backup..." -ForegroundColor Yellow
    $backupPath = Join-Path $PSScriptRoot "backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').bak"
    
    try {
        $backupQuery = "BACKUP DATABASE [$database] TO DISK = '$backupPath' WITH FORMAT, INIT, NAME = 'Full Backup of $database', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
        
        Invoke-Sqlcmd -ServerInstance $server -Database "master" -Username $userId -Password $password -Query $backupQuery -QueryTimeout 300 -TrustServerCertificate
        
        Write-Host "Backup created successfully: $backupPath" -ForegroundColor Green
    }
    catch {
        Write-Host "WARNING: Could not create backup: $_" -ForegroundColor Yellow
        Write-Host "Continuing without backup..." -ForegroundColor Yellow
    }
}

# Read migration script
$scriptPath = Join-Path $PSScriptRoot "Scripts\ConvertPersonConfirmationToBit.sql"
if (-not (Test-Path $scriptPath)) {
    Write-Host "ERROR: Migration script not found at: $scriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "Reading migration script..." -ForegroundColor Yellow
$sqlScript = Get-Content $scriptPath -Raw

# Check current column type
Write-Host "Checking current column type..." -ForegroundColor Yellow
$checkQuery = @"
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'WorkItem' 
AND COLUMN_NAME = 'PersonConfirmation'
"@

try {
    $currentColumn = Invoke-Sqlcmd -ServerInstance $server -Database $database -Username $userId -Password $password -Query $checkQuery -TrustServerCertificate
    
    if ($currentColumn) {
        Write-Host "Current PersonConfirmation column type: $($currentColumn.DATA_TYPE)" -ForegroundColor Cyan
        if ($currentColumn.DATA_TYPE -eq "bit") {
            Write-Host "Column is already BIT type. No migration needed!" -ForegroundColor Green
            exit 0
        }
    }
    else {
        Write-Host "PersonConfirmation column not found. Will create as BIT." -ForegroundColor Yellow
    }
}
catch {
    Write-Host "WARNING: Could not check column type: $_" -ForegroundColor Yellow
}

# Execute migration
Write-Host ""
Write-Host "Executing migration script..." -ForegroundColor Yellow
Write-Host "This will convert PersonConfirmation from NVARCHAR to BIT..." -ForegroundColor Yellow
Write-Host ""

try {
    Invoke-Sqlcmd -ServerInstance $server -Database $database -Username $userId -Password $password -Query $sqlScript -QueryTimeout 300 -TrustServerCertificate
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Migration completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    
    # Verify the change
    Write-Host "Verifying migration..." -ForegroundColor Yellow
    $verifyQuery = @"
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'WorkItem' 
AND COLUMN_NAME = 'PersonConfirmation'
"@
    
    $verifyResult = Invoke-Sqlcmd -ServerInstance $server -Database $database -Username $userId -Password $password -Query $verifyQuery -TrustServerCertificate
    
    if ($verifyResult -and $verifyResult.DATA_TYPE -eq 'bit') {
        Write-Host 'PersonConfirmation is now BIT type' -ForegroundColor Green
        Write-Host ''
        Write-Host 'You can now restart the application. The InvalidCastException should be fixed!' -ForegroundColor Green
    }
    else {
        Write-Host 'WARNING: Verification failed. Please check manually.' -ForegroundColor Yellow
    }
}
catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "ERROR: Migration failed!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Error details: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "If you created a backup, you can restore it using:" -ForegroundColor Yellow
    Write-Host "  RESTORE DATABASE [$database] FROM DISK = '$backupPath'" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green

