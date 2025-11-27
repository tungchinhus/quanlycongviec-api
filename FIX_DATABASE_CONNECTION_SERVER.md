# Hướng Dẫn Fix Database Connection trên Server

## 🔴 Vấn Đề Hiện Tại

**Lỗi:**
```
A network-related or instance-specific error occurred while establishing a connection to SQL Server. 
The server was not found or was not accessible. 
Verify that the instance name is correct and that SQL Server is configured to allow remote connections. 
(provider: SQL Network Interfaces, error: 26 - Error Locating Server/Instance Specified)
```

**Nguyên nhân:** Backend không thể kết nối đến SQL Server database.

---

## ✅ Các Bước Fix

### Bước 1: Kiểm tra SQL Server có đang chạy không

**Trên server, mở PowerShell (Admin):**

```powershell
# Kiểm tra SQL Server service
Get-Service | Where-Object {$_.DisplayName -like "*SQL*"}

# Hoặc kiểm tra cụ thể
Get-Service MSSQLSERVER
Get-Service MSSQL$SQLEXPRESS
```

**Nếu service không chạy, khởi động:**

```powershell
# Khởi động SQL Server (Default instance)
Start-Service MSSQLSERVER

# Hoặc SQL Server Express
Start-Service MSSQL$SQLEXPRESS
```

### Bước 2: Tìm tên SQL Server instance đúng

**Kiểm tra các SQL Server instances đang chạy:**

```powershell
# Liệt kê tất cả SQL Server instances
Get-Service | Where-Object {$_.DisplayName -like "*SQL Server*"} | Select-Object DisplayName, Name, Status

# Hoặc dùng SQL Server Configuration Manager
# Mở: sqlservermanager*.msc (tùy version)
```

**Hoặc test connection:**

```powershell
# Test với SQL Server Management Studio (SSMS)
# Hoặc dùng sqlcmd
sqlcmd -S localhost -E
# Nếu thành công, bạn đang kết nối được
```

### Bước 3: Kiểm tra Database có tồn tại không

**Mở SQL Server Management Studio (SSMS) hoặc dùng sqlcmd:**

```sql
-- Liệt kê tất cả databases
SELECT name FROM sys.databases;

-- Kiểm tra database quanlyphancong có tồn tại không
SELECT name FROM sys.databases WHERE name = 'quanlyphancong';
```

**Nếu database không tồn tại, tạo mới:**

```sql
CREATE DATABASE quanlyphancong;
GO
```

### Bước 4: Cập nhật Connection String trên Server

**Trên server, chỉnh sửa file `appsettings.json`:**

```powershell
# Đường dẫn file
$appsettingsPath = "C:\inetpub\wwwroot\quanlyfilesBE\appsettings.json"

# Backup trước khi sửa
Copy-Item $appsettingsPath "$appsettingsPath.backup"

# Mở file để chỉnh sửa
notepad $appsettingsPath
```

**Các connection string mẫu:**

#### Option 1: SQL Server Default Instance (nếu không có instance name)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=quanlyphancong;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

#### Option 2: SQL Server Express
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=quanlyphancong;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

#### Option 3: SQL Server với SQL Authentication (nếu Integrated Security không hoạt động)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=quanlyphancong;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

#### Option 4: SQL Server trên máy khác (remote)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.1.100\\SQLEXPRESS;Database=quanlyphancong;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

### Bước 5: Cấp quyền cho IIS App Pool Identity

**Nếu dùng Integrated Security, cần cấp quyền cho IIS App Pool:**

```powershell
# Lấy tên App Pool
$appPoolName = "quanlyfilesBE"
$appPoolIdentity = "IIS AppPool\$appPoolName"

# Cấp quyền trong SQL Server
# Mở SSMS và chạy:
```

**SQL Script để cấp quyền:**

```sql
-- Tạo login cho IIS App Pool
CREATE LOGIN [IIS AppPool\quanlyfilesBE] FROM WINDOWS;
GO

-- Cấp quyền cho database
USE quanlyphancong;
GO

CREATE USER [IIS AppPool\quanlyfilesBE] FOR LOGIN [IIS AppPool\quanlyfilesBE];
GO

-- Cấp quyền db_owner (hoặc quyền cụ thể hơn)
ALTER ROLE db_owner ADD MEMBER [IIS AppPool\quanlyfilesBE];
GO
```

**Hoặc dùng SQL Authentication (đơn giản hơn):**

1. Tạo SQL Login trong SSMS:
   - Security → Logins → New Login
   - Chọn "SQL Server authentication"
   - Username: `quanlyfiles_user`
   - Password: (đặt password mạnh)
   - Uncheck "Enforce password policy" (nếu cần)

2. Cấp quyền:
   ```sql
   USE quanlyphancong;
   GO
   CREATE USER quanlyfiles_user FOR LOGIN quanlyfiles_user;
   GO
   ALTER ROLE db_owner ADD MEMBER quanlyfiles_user;
   GO
   ```

3. Cập nhật connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=quanlyphancong;User Id=quanlyfiles_user;Password=YourPassword;TrustServerCertificate=True;"
     }
   }
   ```

### Bước 6: Test Connection String

**Tạo script test:**

```powershell
# Test connection string
$connectionString = "Server=localhost\SQLEXPRESS;Database=quanlyphancong;Integrated Security=True;TrustServerCertificate=True;"

# Test với .NET
Add-Type -AssemblyName System.Data
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
try {
    $connection.Open()
    Write-Host "✅ Connection successful!" -ForegroundColor Green
    $connection.Close()
} catch {
    Write-Host "❌ Connection failed: $($_.Exception.Message)" -ForegroundColor Red
}
```

### Bước 7: Restart IIS Application Pool

**Sau khi sửa connection string:**

```powershell
Import-Module WebAdministration
Restart-WebAppPool -Name "quanlyfilesBE"
Start-Sleep -Seconds 3

# Kiểm tra status
Get-WebAppPoolState -Name "quanlyfilesBE"
```

### Bước 8: Test API Endpoint

**Test database connection endpoint:**

```powershell
# Test endpoint
Invoke-RestMethod -Uri "http://localhost:8080/api/test-db" -Method GET

# Hoặc từ browser
# http://172.20.115.40:8080/api/test-db
```

---

## 🔍 Troubleshooting

### Vấn đề 1: "Server was not found or was not accessible"

**Giải pháp:**
- Kiểm tra SQL Server service đang chạy
- Kiểm tra instance name đúng (SQLEXPRESS, MSSQLSERVER, etc.)
- Kiểm tra SQL Server Browser service đang chạy (nếu dùng named instance)

### Vấn đề 2: "Login failed for user"

**Giải pháp:**
- Nếu dùng Integrated Security: Cấp quyền cho IIS App Pool identity
- Nếu dùng SQL Auth: Kiểm tra username/password đúng
- Kiểm tra SQL Server cho phép SQL Authentication (nếu dùng SQL Auth)

### Vấn đề 3: "Cannot open database"

**Giải pháp:**
- Database không tồn tại → Tạo database
- User không có quyền → Cấp quyền cho user
- Database đang ở trạng thái offline → Set online

### Vấn đề 4: Connection string không được apply

**Giải pháp:**
- Đảm bảo sửa đúng file `appsettings.json` trên server
- Restart IIS Application Pool sau khi sửa
- Kiểm tra không có file `appsettings.Production.json` override

---

## 📋 Checklist

- [ ] SQL Server service đang chạy
- [ ] Database `quanlyphancong` tồn tại
- [ ] Connection string đúng (server name, instance name, database name)
- [ ] User có quyền truy cập database
- [ ] File `appsettings.json` trên server đã được cập nhật
- [ ] IIS Application Pool đã được restart
- [ ] Test endpoint `/api/test-db` trả về success

---

## 🚀 Quick Fix Script

**Tạo file `fix-db-connection.ps1` trên server:**

```powershell
# Fix Database Connection
param(
    [string]$ServerName = "localhost\SQLEXPRESS",
    [string]$DatabaseName = "quanlyphancong",
    [string]$AppPoolName = "quanlyfilesBE"
)

Write-Host "=== Fix Database Connection ===" -ForegroundColor Green

# 1. Check SQL Server service
Write-Host "[1/4] Checking SQL Server service..." -ForegroundColor Cyan
$sqlService = Get-Service | Where-Object {$_.DisplayName -like "*SQL Server*" -and $_.Status -eq "Running"} | Select-Object -First 1
if ($sqlService) {
    Write-Host "✅ SQL Server is running: $($sqlService.DisplayName)" -ForegroundColor Green
} else {
    Write-Host "❌ SQL Server is not running!" -ForegroundColor Red
    Write-Host "Please start SQL Server service manually" -ForegroundColor Yellow
    exit 1
}

# 2. Update appsettings.json
Write-Host "[2/4] Updating appsettings.json..." -ForegroundColor Cyan
$appsettingsPath = "C:\inetpub\wwwroot\quanlyfilesBE\appsettings.json"
if (-not (Test-Path $appsettingsPath)) {
    Write-Host "❌ appsettings.json not found at: $appsettingsPath" -ForegroundColor Red
    exit 1
}

# Backup
Copy-Item $appsettingsPath "$appsettingsPath.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"

# Read and update
$json = Get-Content $appsettingsPath | ConvertFrom-Json
$json.ConnectionStrings.DefaultConnection = "Server=$ServerName;Database=$DatabaseName;Integrated Security=True;TrustServerCertificate=True;"
$json | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath -Encoding UTF8

Write-Host "✅ Updated connection string" -ForegroundColor Green
Write-Host "   Server: $ServerName" -ForegroundColor Cyan
Write-Host "   Database: $DatabaseName" -ForegroundColor Cyan

# 3. Restart App Pool
Write-Host "[3/4] Restarting IIS Application Pool..." -ForegroundColor Cyan
Import-Module WebAdministration -ErrorAction SilentlyContinue
Restart-WebAppPool -Name $AppPoolName
Start-Sleep -Seconds 3

$poolState = Get-WebAppPoolState -Name $AppPoolName
if ($poolState.Value -eq "Started") {
    Write-Host "✅ Application Pool restarted" -ForegroundColor Green
} else {
    Write-Host "❌ Application Pool failed to start" -ForegroundColor Red
}

# 4. Test connection
Write-Host "[4/4] Testing connection..." -ForegroundColor Cyan
Start-Sleep -Seconds 2
try {
    $response = Invoke-RestMethod -Uri "http://localhost:8080/api/test-db" -Method GET -ErrorAction Stop
    if ($response.success) {
        Write-Host "✅ Database connection successful!" -ForegroundColor Green
        Write-Host "   Database: $($response.databaseName)" -ForegroundColor Cyan
        Write-Host "   Tables: $($response.tableCount)" -ForegroundColor Cyan
    } else {
        Write-Host "❌ Database connection failed: $($response.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Cannot test connection: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Please test manually at: http://localhost:8080/api/test-db" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Fix Complete ===" -ForegroundColor Green
```

**Cách dùng:**

```powershell
# Chạy với default settings
.\fix-db-connection.ps1

# Hoặc chỉ định server/database
.\fix-db-connection.ps1 -ServerName "localhost" -DatabaseName "quanlyphancong"
```

---

## 📝 Lưu Ý

1. **Backup trước khi sửa:** Luôn backup `appsettings.json` trước khi chỉnh sửa
2. **Test connection:** Luôn test connection sau khi sửa
3. **Security:** Nếu dùng SQL Authentication, đảm bảo password mạnh
4. **Firewall:** Nếu SQL Server trên máy khác, kiểm tra firewall rules

---

Sau khi fix xong, test lại API endpoint `/api/auth/login/firebase-token` từ frontend!









