# Script đơn giản để chạy SQL script CreateTechnicalSheetApprovalTable.sql
# Sử dụng: .\run-sql-migration.ps1

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Running TechnicalSheetApproval Migration" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Đường dẫn đến SQL script
$scriptPath = Join-Path $PSScriptRoot "CreateTechnicalSheetApprovalTable.sql"

if (-not (Test-Path $scriptPath)) {
    Write-Host "ERROR: SQL script not found at: $scriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "SQL Script: $scriptPath" -ForegroundColor Yellow
Write-Host ""
Write-Host "Please run this SQL script in SQL Server Management Studio (SSMS)" -ForegroundColor Yellow
Write-Host "or using sqlcmd command line tool." -ForegroundColor Yellow
Write-Host ""
Write-Host "Example sqlcmd command:" -ForegroundColor Cyan
Write-Host "  sqlcmd -S . -d quanlyphancong -U sa -P 123456 -i `"$scriptPath`"" -ForegroundColor White
Write-Host ""
Write-Host "Or open the file in SSMS and execute it manually." -ForegroundColor Yellow
Write-Host ""

# Hỏi user có muốn chạy bằng sqlcmd không
$runSqlCmd = Read-Host "Do you want to try running with sqlcmd? (Y/N)"

if ($runSqlCmd -eq "Y" -or $runSqlCmd -eq "y") {
    try {
        # Thử tìm sqlcmd
        $sqlcmd = Get-Command sqlcmd -ErrorAction Stop
        
        Write-Host "Found sqlcmd, attempting to run script..." -ForegroundColor Green
        
        # Connection string từ appsettings.json (default)
        $server = "."
        $database = "quanlyphancong"
        $user = "sa"
        $password = "123456"
        
        $sqlcmdArgs = @(
            "-S", $server
            "-d", $database
            "-U", $user
            "-P", $password
            "-i", $scriptPath
        )
        
        & $sqlcmd $sqlcmdArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "=========================================" -ForegroundColor Green
            Write-Host "SQL script executed successfully!" -ForegroundColor Green
            Write-Host "=========================================" -ForegroundColor Green
            Write-Host ""
            Write-Host "Next: Run EF Core migration (after stopping backend):" -ForegroundColor Yellow
            Write-Host "  dotnet ef database update" -ForegroundColor White
        } else {
            Write-Host ""
            Write-Host "ERROR: sqlcmd exited with code $LASTEXITCODE" -ForegroundColor Red
            Write-Host "Please run the SQL script manually in SSMS." -ForegroundColor Yellow
        }
    } catch {
        Write-Host ""
        Write-Host "sqlcmd not found. Please run the SQL script manually:" -ForegroundColor Yellow
        Write-Host "  File: $scriptPath" -ForegroundColor Cyan
    }
} else {
    Write-Host "Please run the SQL script manually in SSMS." -ForegroundColor Yellow
    Write-Host "  File: $scriptPath" -ForegroundColor Cyan
}
