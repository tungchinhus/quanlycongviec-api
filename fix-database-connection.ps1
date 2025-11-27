# Script kiểm tra và sửa lỗi kết nối database
# Chạy với quyền Administrator

param(
    [string]$PublishPath = "C:\inetpub\wwwroot\quanlyfilesBE",
    [string]$AppPoolName = "quanlyfilesBE"
)

Write-Host "=== Kiểm Tra và Sửa Lỗi Kết Nối Database ===" -ForegroundColor Green
Write-Host ""

$appSettingsPath = Join-Path $PublishPath "appsettings.json"
$appSettingsProdPath = Join-Path $PublishPath "appsettings.Production.json"

# Kiểm tra file appsettings.json
Write-Host "[1/5] Kiểm tra file appsettings.json..." -ForegroundColor Yellow
if (-not (Test-Path $appSettingsPath)) {
    Write-Host "✗ File appsettings.json không tồn tại tại: $appSettingsPath" -ForegroundColor Red
    Write-Host "  Hãy đảm bảo đã publish application!" -ForegroundColor Yellow
    exit 1
}
Write-Host "✓ File appsettings.json tồn tại" -ForegroundColor Green

# Đọc connection string hiện tại
Write-Host "`n[2/5] Đọc connection string hiện tại..." -ForegroundColor Yellow
try {
    $appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
    $currentConnectionString = $appSettings.ConnectionStrings.DefaultConnection
    Write-Host "Connection String hiện tại:" -ForegroundColor Cyan
    Write-Host "  $currentConnectionString" -ForegroundColor White
} catch {
    Write-Host "✗ Lỗi khi đọc appsettings.json: $_" -ForegroundColor Red
    exit 1
}

# Kiểm tra SQL Server đang chạy
Write-Host "`n[3/5] Kiểm tra SQL Server..." -ForegroundColor Yellow
$sqlServices = Get-Service -Name "MSSQL*" -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq "Running" }
if ($sqlServices) {
    Write-Host "✓ SQL Server đang chạy:" -ForegroundColor Green
    foreach ($service in $sqlServices) {
        Write-Host "  - $($service.Name): $($service.DisplayName)" -ForegroundColor Cyan
    }
} else {
    Write-Host "⚠ Không tìm thấy SQL Server đang chạy" -ForegroundColor Yellow
    Write-Host "  Hãy kiểm tra SQL Server đã được cài đặt và đang chạy chưa" -ForegroundColor Yellow
}

# Phân tích connection string
Write-Host "`n[4/5] Phân tích connection string..." -ForegroundColor Yellow
if ($currentConnectionString -like "*Integrated Security=True*") {
    Write-Host "⚠ Connection string đang dùng Windows Authentication" -ForegroundColor Yellow
    Write-Host "  Application Pool Identity có thể không có quyền truy cập SQL Server" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Có 2 cách giải quyết:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "CÁCH 1: Cấp quyền cho Application Pool Identity (Khuyến nghị)" -ForegroundColor Green
    Write-Host "  1. Mở SQL Server Management Studio" -ForegroundColor White
    Write-Host "  2. Kết nối đến SQL Server" -ForegroundColor White
    Write-Host "  3. Security → Logins → New Login" -ForegroundColor White
    Write-Host "  4. Tìm và thêm: IIS AppPool\$AppPoolName" -ForegroundColor White
    Write-Host "  5. Server Roles: dbcreator, public" -ForegroundColor White
    Write-Host "  6. User Mapping: Chọn database 'quanlyphancong', Role: db_owner" -ForegroundColor White
    Write-Host ""
    Write-Host "CÁCH 2: Đổi sang SQL Server Authentication" -ForegroundColor Green
    Write-Host "  Sẽ cập nhật connection string để dùng User/Password" -ForegroundColor White
    Write-Host ""
    
    $choice = Read-Host "Chọn cách (1 hoặc 2, Enter để bỏ qua)"
    
    if ($choice -eq "2") {
        Write-Host "`nNhập thông tin SQL Server Authentication:" -ForegroundColor Cyan
        $server = Read-Host "Server (ví dụ: localhost\SQLEXPRESS hoặc .\SQLEXPRESS)"
        $database = Read-Host "Database (mặc định: quanlyphancong)" 
        if ([string]::IsNullOrWhiteSpace($database)) {
            $database = "quanlyphancong"
        }
        $userId = Read-Host "User ID (ví dụ: sa)"
        $password = Read-Host "Password" -AsSecureString
        $passwordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))
        
        $newConnectionString = "Server=$server;Database=$database;User Id=$userId;Password=$passwordPlain;TrustServerCertificate=True;MultipleActiveResultSets=true"
        
        Write-Host "`nConnection string mới:" -ForegroundColor Cyan
        Write-Host "  $newConnectionString" -ForegroundColor White
        
        $confirm = Read-Host "`nXác nhận cập nhật? (Y/N)"
        if ($confirm -eq "Y" -or $confirm -eq "y") {
            # Backup file cũ
            $backupPath = "$appSettingsPath.backup.$(Get-Date -Format 'yyyyMMddHHmmss')"
            Copy-Item $appSettingsPath $backupPath -Force
            Write-Host "✓ Đã backup file cũ: $backupPath" -ForegroundColor Green
            
            # Cập nhật connection string
            $appSettings.ConnectionStrings.DefaultConnection = $newConnectionString
            $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath -Encoding UTF8
            Write-Host "✓ Đã cập nhật connection string" -ForegroundColor Green
        }
    } elseif ($choice -eq "1") {
        Write-Host "`nHãy làm theo hướng dẫn trên để cấp quyền cho Application Pool Identity" -ForegroundColor Yellow
        Write-Host "Sau đó restart Application Pool và kiểm tra lại" -ForegroundColor Yellow
    }
} else {
    Write-Host "✓ Connection string đang dùng SQL Server Authentication" -ForegroundColor Green
}

# Kiểm tra database có tồn tại không
Write-Host "`n[5/5] Kiểm tra database..." -ForegroundColor Yellow
if ($currentConnectionString -match "Database=(\w+)") {
    $dbName = $matches[1]
    Write-Host "Database name: $dbName" -ForegroundColor Cyan
    
    # Thử kết nối (nếu có sqlcmd)
    $sqlcmdPath = "C:\Program Files\Microsoft SQL Server\*\Tools\Binn\sqlcmd.exe"
    $sqlcmd = Get-ChildItem -Path $sqlcmdPath -ErrorAction SilentlyContinue | Select-Object -First 1
    
    if ($sqlcmd) {
        Write-Host "  (Có thể kiểm tra database bằng SQL Server Management Studio)" -ForegroundColor Cyan
    }
}

Write-Host "`n=== Hoàn tất! ===" -ForegroundColor Green
Write-Host "`nCác bước tiếp theo:" -ForegroundColor Yellow
Write-Host "1. Đảm bảo SQL Server đang chạy" -ForegroundColor White
Write-Host "2. Đảm bảo database 'quanlyphancong' đã được tạo" -ForegroundColor White
Write-Host "3. Cấp quyền cho Application Pool Identity hoặc dùng SQL Authentication" -ForegroundColor White
Write-Host "4. Restart Application Pool trong IIS Manager" -ForegroundColor White
Write-Host "5. Kiểm tra lại endpoint: http://localhost:8080/api/test-db" -ForegroundColor White
Write-Host ""
Write-Host "Nếu database chưa tồn tại, chạy migration:" -ForegroundColor Yellow
Write-Host "  cd D:\Project\thibidi\quanlyfiles\quanlyfilesBE" -ForegroundColor Cyan
Write-Host "  dotnet ef database update" -ForegroundColor Cyan

