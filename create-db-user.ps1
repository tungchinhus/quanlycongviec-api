# Script tạo SQL User mới và cập nhật connection string
# Chạy với quyền Administrator

param(
    [string]$ServerName = "localhost\SQLEXPRESS",
    [string]$DatabaseName = "quanlyphancong",
    [string]$UserName = "quanlyfiles_user",
    [string]$Password = ""
)

Write-Host "=== Tạo SQL User Mới cho Database ===" -ForegroundColor Green
Write-Host ""

# Nhập thông tin nếu chưa có
if ([string]::IsNullOrWhiteSpace($Password)) {
    Write-Host "Nhập thông tin để tạo SQL User:" -ForegroundColor Cyan
    $ServerName = Read-Host "Server name (mặc định: localhost\SQLEXPRESS)"
    if ([string]::IsNullOrWhiteSpace($ServerName)) {
        $ServerName = "localhost\SQLEXPRESS"
    }
    
    $DatabaseName = Read-Host "Database name (mặc định: quanlyphancong)"
    if ([string]::IsNullOrWhiteSpace($DatabaseName)) {
        $DatabaseName = "quanlyphancong"
    }
    
    $UserName = Read-Host "Username (mặc định: quanlyfiles_user)"
    if ([string]::IsNullOrWhiteSpace($UserName)) {
        $UserName = "quanlyfiles_user"
    }
    
    $Password = Read-Host "Password (tối thiểu 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt)" -AsSecureString
    $PasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password))
} else {
    $PasswordPlain = $Password
}

Write-Host "`nThông tin sẽ được tạo:" -ForegroundColor Yellow
Write-Host "  Server: $ServerName" -ForegroundColor White
Write-Host "  Database: $DatabaseName" -ForegroundColor White
Write-Host "  Username: $UserName" -ForegroundColor White
Write-Host "  Password: [đã nhập]" -ForegroundColor White
Write-Host ""

$confirm = Read-Host "Xác nhận tạo user? (Y/N)"
if ($confirm -ne "Y" -and $confirm -ne "y") {
    Write-Host "Đã hủy." -ForegroundColor Yellow
    exit 0
}

# Tạo SQL script
$sqlScript = @"
USE [master]
GO

-- Tạo Login mới
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = '$UserName')
BEGIN
    CREATE LOGIN [$UserName] WITH PASSWORD = '$($PasswordPlain.Replace("'", "''"))', 
        DEFAULT_DATABASE = [$DatabaseName],
        CHECK_EXPIRATION = OFF,
        CHECK_POLICY = ON
    PRINT 'Login [$UserName] đã được tạo thành công!'
END
ELSE
BEGIN
    PRINT 'Login [$UserName] đã tồn tại.'
END
GO

-- Cấp quyền server roles
IF EXISTS (SELECT * FROM sys.server_principals WHERE name = '$UserName')
BEGIN
    ALTER SERVER ROLE [dbcreator] ADD MEMBER [$UserName]
    PRINT 'Đã cấp quyền dbcreator cho [$UserName]'
END
GO

-- Chuyển sang database
USE [$DatabaseName]
GO

-- Tạo User trong database
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = '$UserName')
BEGIN
    CREATE USER [$UserName] FOR LOGIN [$UserName]
    PRINT 'User [$UserName] đã được tạo trong database [$DatabaseName]'
END
ELSE
BEGIN
    PRINT 'User [$UserName] đã tồn tại trong database [$DatabaseName]'
END
GO

-- Cấp quyền db_owner
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = '$UserName')
BEGIN
    ALTER ROLE [db_owner] ADD MEMBER [$UserName]
    PRINT 'Đã cấp quyền db_owner cho [$UserName]'
END
GO

PRINT ''
PRINT '=== Hoàn tất! ==='
PRINT 'Username: $UserName'
PRINT 'Password: [đã nhập]'
GO
"@

# Lưu SQL script tạm thời
$tempSqlFile = Join-Path $env:TEMP "create_user_$(Get-Date -Format 'yyyyMMddHHmmss').sql"
$sqlScript | Out-File -FilePath $tempSqlFile -Encoding UTF8

Write-Host "`nĐang tạo user trong SQL Server..." -ForegroundColor Yellow

# Tìm sqlcmd
$sqlcmdPath = "C:\Program Files\Microsoft SQL Server\*\Tools\Binn\sqlcmd.exe"
$sqlcmd = Get-ChildItem -Path $sqlcmdPath -ErrorAction SilentlyContinue | Select-Object -First 1

if ($sqlcmd) {
    # Chạy SQL script bằng sqlcmd với Windows Authentication (cần quyền sysadmin)
    $serverInstance = $ServerName
    $result = & $sqlcmd.FullName -S $serverInstance -E -i $tempSqlFile 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ User đã được tạo thành công!" -ForegroundColor Green
        Write-Host $result -ForegroundColor Cyan
    } else {
        Write-Host "✗ Lỗi khi tạo user:" -ForegroundColor Red
        Write-Host $result -ForegroundColor Red
        Write-Host "`nHãy chạy script SQL thủ công trong SQL Server Management Studio:" -ForegroundColor Yellow
        Write-Host "  File: $tempSqlFile" -ForegroundColor Cyan
        exit 1
    }
} else {
    Write-Host "⚠ Không tìm thấy sqlcmd.exe" -ForegroundColor Yellow
    Write-Host "Hãy chạy script SQL thủ công trong SQL Server Management Studio:" -ForegroundColor Yellow
    Write-Host "  File: $tempSqlFile" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Hoặc copy nội dung sau và chạy trong SSMS:" -ForegroundColor Yellow
    Write-Host $sqlScript -ForegroundColor White
    Write-Host ""
    Read-Host "Nhấn Enter sau khi đã chạy script SQL..."
}

# Xóa file tạm
Remove-Item $tempSqlFile -ErrorAction SilentlyContinue

# Cập nhật appsettings.Development.json
Write-Host "`nCập nhật connection string trong appsettings.Development.json..." -ForegroundColor Yellow

$appSettingsPath = Join-Path $PSScriptRoot "appsettings.Development.json"

if (Test-Path $appSettingsPath) {
    try {
        $appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
        
        # Escape password cho connection string
        $escapedPassword = $PasswordPlain.Replace(';', '&#59;')
        
        $newConnectionString = "Server=$ServerName;Database=$DatabaseName;User Id=$UserName;Password=$escapedPassword;TrustServerCertificate=True;MultipleActiveResultSets=true"
        
        $appSettings.ConnectionStrings.DefaultConnection = $newConnectionString
        $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath -Encoding UTF8
        
        Write-Host "✓ Đã cập nhật connection string trong appsettings.Development.json" -ForegroundColor Green
        Write-Host "  Connection String: Server=$ServerName;Database=$DatabaseName;User Id=$UserName;Password=***" -ForegroundColor Cyan
    } catch {
        Write-Host "✗ Lỗi khi cập nhật appsettings.Development.json: $_" -ForegroundColor Red
        Write-Host "Hãy cập nhật thủ công:" -ForegroundColor Yellow
        Write-Host "  Server=$ServerName;Database=$DatabaseName;User Id=$UserName;Password=[password đã nhập];TrustServerCertificate=True;MultipleActiveResultSets=true" -ForegroundColor White
    }
} else {
    Write-Host "⚠ Không tìm thấy appsettings.Development.json" -ForegroundColor Yellow
    Write-Host "Hãy cập nhật thủ công connection string:" -ForegroundColor Yellow
    Write-Host "  Server=$ServerName;Database=$DatabaseName;User Id=$UserName;Password=[password đã nhập];TrustServerCertificate=True;MultipleActiveResultSets=true" -ForegroundColor White
}

Write-Host "`n=== Hoàn tất! ===" -ForegroundColor Green
Write-Host "`nThông tin đăng nhập:" -ForegroundColor Yellow
Write-Host "  Username: $UserName" -ForegroundColor White
Write-Host "  Password: [password đã nhập]" -ForegroundColor White
Write-Host "`nHãy restart backend và thử đăng nhập lại!" -ForegroundColor Cyan

