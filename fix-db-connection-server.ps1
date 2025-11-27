# Fix Database Connection trên Server
# Chạy với quyền Administrator

param(
    [string]$ServerName = "localhost\SQLEXPRESS",
    [string]$DatabaseName = "quanlyphancong",
    [string]$AppPoolName = "quanlyfilesBE",
    [string]$AppPath = "C:\inetpub\wwwroot\quanlyfilesBE"
)

Write-Host "=== Fix Database Connection ===" -ForegroundColor Green

# Kiểm tra quyền Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: Script phải chạy với quyền Administrator!" -ForegroundColor Red
    exit 1
}

# 1. Check SQL Server service
Write-Host "[1/5] Checking SQL Server service..." -ForegroundColor Cyan
$sqlServices = Get-Service | Where-Object {$_.DisplayName -like "*SQL Server*" -and $_.Status -eq "Running"}
if ($sqlServices) {
    Write-Host "✅ SQL Server is running:" -ForegroundColor Green
    $sqlServices | ForEach-Object { Write-Host "   - $($_.DisplayName)" -ForegroundColor Cyan }
} else {
    Write-Host "⚠️  SQL Server service not found or not running" -ForegroundColor Yellow
    Write-Host "   Please ensure SQL Server is installed and running" -ForegroundColor Yellow
}

# 2. Check appsettings.json exists
Write-Host "[2/5] Checking appsettings.json..." -ForegroundColor Cyan
$appsettingsPath = Join-Path $AppPath "appsettings.json"
if (-not (Test-Path $appsettingsPath)) {
    Write-Host "❌ appsettings.json not found at: $appsettingsPath" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Found appsettings.json" -ForegroundColor Green

# 3. Backup appsettings.json
Write-Host "[3/5] Backing up appsettings.json..." -ForegroundColor Cyan
$backupPath = "$appsettingsPath.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item $appsettingsPath $backupPath
Write-Host "✅ Backup created: $backupPath" -ForegroundColor Green

# 4. Update connection string
Write-Host "[4/5] Updating connection string..." -ForegroundColor Cyan
try {
    $jsonContent = Get-Content $appsettingsPath -Raw -Encoding UTF8
    $json = $jsonContent | ConvertFrom-Json
    
    # Update connection string
    $newConnectionString = "Server=$ServerName;Database=$DatabaseName;Integrated Security=True;TrustServerCertificate=True;"
    $json.ConnectionStrings.DefaultConnection = $newConnectionString
    
    # Convert back to JSON and save
    $json | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath -Encoding UTF8
    
    Write-Host "✅ Connection string updated:" -ForegroundColor Green
    Write-Host "   Server: $ServerName" -ForegroundColor Cyan
    Write-Host "   Database: $DatabaseName" -ForegroundColor Cyan
    Write-Host "   Connection String: $newConnectionString" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Failed to update appsettings.json: $($_.Exception.Message)" -ForegroundColor Red
    # Restore backup
    Copy-Item $backupPath $appsettingsPath -Force
    Write-Host "✅ Restored from backup" -ForegroundColor Yellow
    exit 1
}

# 5. Restart IIS Application Pool
Write-Host "[5/5] Restarting IIS Application Pool..." -ForegroundColor Cyan
Import-Module WebAdministration -ErrorAction SilentlyContinue

if (-not (Get-Module WebAdministration)) {
    Write-Host "⚠️  Cannot import WebAdministration module" -ForegroundColor Yellow
    Write-Host "   Please restart Application Pool manually in IIS Manager" -ForegroundColor Yellow
} else {
    try {
        Restart-WebAppPool -Name $AppPoolName
        Start-Sleep -Seconds 3
        
        $poolState = Get-WebAppPoolState -Name $AppPoolName
        if ($poolState.Value -eq "Started") {
            Write-Host "✅ Application Pool restarted successfully" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Application Pool state: $($poolState.Value)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "⚠️  Failed to restart Application Pool: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "   Please restart manually in IIS Manager" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== Fix Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Test database connection:" -ForegroundColor White
Write-Host "   http://localhost:8080/api/test-db" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. If connection still fails, check:" -ForegroundColor White
Write-Host "   - SQL Server service is running" -ForegroundColor Cyan
Write-Host "   - Database '$DatabaseName' exists" -ForegroundColor Cyan
Write-Host "   - IIS App Pool identity has database permissions" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. If using SQL Authentication, update connection string manually:" -ForegroundColor White
Write-Host "   Server=$ServerName;Database=$DatabaseName;User Id=username;Password=password;TrustServerCertificate=True;" -ForegroundColor Cyan
Write-Host ""
Write-Host "Backup location: $backupPath" -ForegroundColor Gray









