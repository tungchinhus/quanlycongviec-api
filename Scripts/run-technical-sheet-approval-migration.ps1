# Script để chạy SQL migration cho TechnicalSheetApproval table
# Sử dụng connection string từ appsettings.json

$ErrorActionPreference = "Stop"

# Đọc connection string từ appsettings.json
$appsettingsPath = Join-Path $PSScriptRoot "..\appsettings.json"
$appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
$connectionString = $appsettings.ConnectionStrings.DefaultConnection

# Parse connection string để lấy thông tin
$server = ""
$database = ""
$userId = ""
$password = ""

if ($connectionString -match "Server=([^;]+)") {
    $server = $matches[1]
}
if ($connectionString -match "Database=([^;]+)") {
    $database = $matches[1]
}
if ($connectionString -match "User Id=([^;]+)") {
    $userId = $matches[1]
}
if ($connectionString -match "Password=([^;]+)") {
    $password = $matches[1]
}

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Running TechnicalSheetApproval Migration" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Server: $server" -ForegroundColor Yellow
Write-Host "Database: $database" -ForegroundColor Yellow
Write-Host ""

# Đường dẫn đến SQL script
$sqlScriptPath = Join-Path $PSScriptRoot "CreateTechnicalSheetApprovalTable.sql"

if (-not (Test-Path $sqlScriptPath)) {
    Write-Host "ERROR: SQL script not found at: $sqlScriptPath" -ForegroundColor Red
    exit 1
}

# Đọc nội dung SQL script
$sqlScript = Get-Content $sqlScriptPath -Raw

Write-Host "Executing SQL script..." -ForegroundColor Green

try {
    # Sử dụng sqlcmd để chạy script
    $sqlcmdPath = "sqlcmd"
    
    # Kiểm tra xem sqlcmd có sẵn không
    $sqlcmdExists = Get-Command $sqlcmdPath -ErrorAction SilentlyContinue
    
    if (-not $sqlcmdExists) {
        Write-Host "WARNING: sqlcmd not found in PATH. Trying alternative method..." -ForegroundColor Yellow
        
        # Thử dùng Invoke-Sqlcmd nếu có SQL Server PowerShell module
        if (Get-Module -ListAvailable -Name SqlServer) {
            Import-Module SqlServer -ErrorAction SilentlyContinue
            Write-Host "Using Invoke-Sqlcmd from SqlServer module..." -ForegroundColor Yellow
            
            # Tách connection string thành các tham số
            $sqlParams = @{
                ServerInstance = $server
                Database = $database
                Username = $userId
                Password = $password
                Query = $sqlScript
            }
            
            Invoke-Sqlcmd @sqlParams -ErrorAction Stop
            Write-Host "SQL script executed successfully!" -ForegroundColor Green
        } else {
            Write-Host "ERROR: Neither sqlcmd nor SqlServer PowerShell module found." -ForegroundColor Red
            Write-Host "Please install SQL Server Command Line Utilities or run the SQL script manually in SSMS." -ForegroundColor Yellow
            Write-Host ""
            Write-Host "SQL Script location: $sqlScriptPath" -ForegroundColor Cyan
            exit 1
        }
    } else {
        # Sử dụng sqlcmd
        $sqlcmdArgs = @(
            "-S", $server
            "-d", $database
            "-U", $userId
            "-P", $password
            "-i", $sqlScriptPath
        )
        
        & $sqlcmdPath $sqlcmdArgs
        if ($LASTEXITCODE -ne 0) {
            throw "sqlcmd exited with code $LASTEXITCODE"
        }
        Write-Host "SQL script executed successfully!" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "Migration completed successfully!" -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Run EF Core migration: dotnet ef database update" -ForegroundColor White
    Write-Host "   (Or the migration will be applied automatically on next app start)" -ForegroundColor Gray
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "ERROR: Failed to execute SQL script" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "You can also run the SQL script manually in SQL Server Management Studio:" -ForegroundColor Yellow
    Write-Host "  File: $sqlScriptPath" -ForegroundColor Cyan
    exit 1
}
