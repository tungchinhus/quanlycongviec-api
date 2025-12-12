# Script kiem tra va sua loi database khong the truy cap
# Chay voi quyen Administrator

param(
    [string]$ServerName = "localhost\SQLEXPRESS",
    [string]$DatabaseName = "quanlyphancong"
)

Write-Host "=== Kiem Tra va Sua Loi Database Khong The Truy Cap ===" -ForegroundColor Green
Write-Host ""

# Tim sqlcmd
$sqlcmdPath = "C:\Program Files\Microsoft SQL Server\*\Tools\Binn\sqlcmd.exe"
$sqlcmd = Get-ChildItem -Path $sqlcmdPath -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $sqlcmd) {
    Write-Host "Khong tim thay sqlcmd.exe" -ForegroundColor Red
    Write-Host "Hay chay script SQL thu cong trong SQL Server Management Studio:" -ForegroundColor Yellow
    Write-Host "  File: fix-database-access.sql" -ForegroundColor Cyan
    exit 1
}

Write-Host "Dang kiem tra trang thai database..." -ForegroundColor Yellow

# Kiem tra trang thai database
$checkStatusQuery = @"
SELECT 
    name,
    state_desc,
    user_access_desc,
    is_read_only
FROM sys.databases
WHERE name = '$DatabaseName'
"@

$statusFile = Join-Path $env:TEMP "check_db_status_$(Get-Date -Format 'yyyyMMddHHmmss').sql"
$checkStatusQuery | Out-File -FilePath $statusFile -Encoding UTF8

$statusResult = & $sqlcmd.FullName -S $ServerName -E -i $statusFile 2>&1
Remove-Item $statusFile -ErrorAction SilentlyContinue

Write-Host $statusResult -ForegroundColor Cyan

# Tao SQL script de sua
$sqlScript = @"
USE [master]
GO

-- Kiem tra va sua database OFFLINE
IF EXISTS (SELECT * FROM sys.databases WHERE name = '$DatabaseName' AND state_desc = 'OFFLINE')
BEGIN
    PRINT 'Database $DatabaseName dang OFFLINE. Dang set lai ONLINE...'
    ALTER DATABASE [$DatabaseName] SET ONLINE
    PRINT 'Da set database ONLINE.'
END
GO

-- Kiem tra va sua database RESTRICTED_USER
IF EXISTS (SELECT * FROM sys.databases WHERE name = '$DatabaseName' AND user_access_desc = 'RESTRICTED')
BEGIN
    PRINT 'Database $DatabaseName dang o che do RESTRICTED_USER. Dang set lai MULTI_USER...'
    ALTER DATABASE [$DatabaseName] SET MULTI_USER
    PRINT 'Da set database ve MULTI_USER.'
END
GO

-- Kiem tra va sua database SUSPECT
IF EXISTS (SELECT * FROM sys.databases WHERE name = '$DatabaseName' AND state_desc = 'SUSPECT')
BEGIN
    PRINT 'Database $DatabaseName dang o trang thai SUSPECT. Dang thu sua...'
    ALTER DATABASE [$DatabaseName] SET EMERGENCY
    GO
    ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
    GO
    DBCC CHECKDB ([$DatabaseName], REPAIR_ALLOW_DATA_LOSS)
    GO
    ALTER DATABASE [$DatabaseName] SET MULTI_USER
    GO
    ALTER DATABASE [$DatabaseName] SET ONLINE
    GO
    PRINT 'Da thu sua database.'
END
GO

-- Kiem tra lai trang thai
SELECT 
    name AS [Database Name],
    state_desc AS [State],
    user_access_desc AS [User Access]
FROM sys.databases
WHERE name = '$DatabaseName'
GO
"@

$fixFile = Join-Path $env:TEMP "fix_db_access_$(Get-Date -Format 'yyyyMMddHHmmss').sql"
$sqlScript | Out-File -FilePath $fixFile -Encoding UTF8

Write-Host "`nDang thu sua database..." -ForegroundColor Yellow

$fixResult = & $sqlcmd.FullName -S $ServerName -E -i $fixFile 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "Da thu sua database" -ForegroundColor Green
    Write-Host $fixResult -ForegroundColor Cyan
} else {
    Write-Host "Co loi khi sua database:" -ForegroundColor Yellow
    Write-Host $fixResult -ForegroundColor Red
    Write-Host "`nHay chay script SQL thu cong trong SQL Server Management Studio:" -ForegroundColor Yellow
    Write-Host "  File: fix-database-access.sql" -ForegroundColor Cyan
}

Remove-Item $fixFile -ErrorAction SilentlyContinue

Write-Host "`n=== Hoan tat! ===" -ForegroundColor Green
Write-Host "`nCac buoc tiep theo:" -ForegroundColor Yellow
Write-Host "1. Mo SQL Server Management Studio va kiem tra lai database" -ForegroundColor White
Write-Host "2. Neu van loi, chay script fix-database-access.sql thu cong" -ForegroundColor White
Write-Host "3. Neu database bi corrupted, co the can restore tu backup" -ForegroundColor White
Write-Host "4. Sau khi sua, restart backend va thu lai" -ForegroundColor White
