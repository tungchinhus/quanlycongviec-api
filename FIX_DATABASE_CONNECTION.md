# Hướng Dẫn Sửa Lỗi Kết Nối Database

## Lỗi: "Database Connection Failed" - "Cannot connect to database"

### Nguyên nhân:
- Application Pool Identity không có quyền truy cập SQL Server
- Database chưa được tạo
- SQL Server chưa được cài đặt hoặc chưa chạy
- Connection string không đúng

## Giải pháp:

### Cách 1: Sử dụng Script (Khuyến nghị)

```powershell
cd D:\Project\thibidi\quanlyfiles\quanlyfilesBE
.\fix-database-connection.ps1
```

### Cách 2: Cấp quyền cho Application Pool Identity (Windows Authentication)

1. **Mở SQL Server Management Studio (SSMS)**
2. **Kết nối đến SQL Server** (localhost\SQLEXPRESS hoặc tên instance của bạn)
3. **Tạo Login cho Application Pool Identity:**
   - Expand **Security** → Right-click **Logins** → **New Login**
   - Click **Search...**
   - Nhập: `IIS AppPool\quanlyfilesBE`
   - Click **Check Names** → **OK**
   - Click **OK** để tạo login

4. **Cấp quyền Server:**
   - Trong tab **Server Roles**, chọn:
     - `dbcreator` (để tạo database nếu cần)
     - `public`

5. **Cấp quyền Database:**
   - Trong tab **User Mapping**, chọn database `quanlyphancong`
   - Database role membership: Chọn `db_owner`
   - Click **OK**

6. **Restart Application Pool** trong IIS Manager

### Cách 3: Đổi sang SQL Server Authentication

1. **Tạo SQL Login trong SSMS:**
   - Security → Logins → New Login
   - Chọn **SQL Server authentication**
   - Nhập Login name và Password
   - Server Roles: `dbcreator`, `public`
   - User Mapping: Chọn database `quanlyphancong`, Role: `db_owner`

2. **Cập nhật appsettings.json trong thư mục publish:**

   Mở file: `C:\inetpub\wwwroot\quanlyfilesBE\appsettings.json`

   Thay đổi connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=quanlyphancong;User Id=your_username;Password=your_password;TrustServerCertificate=True;MultipleActiveResultSets=true"
     }
   }
   ```

3. **Restart Application Pool** trong IIS Manager

### Cách 4: Tạo Database nếu chưa có

Nếu database `quanlyphancong` chưa tồn tại:

1. **Chạy migration từ project:**
   ```powershell
   cd D:\Project\thibidi\quanlyfiles\quanlyfilesBE
   dotnet ef database update
   ```

2. **Hoặc tạo database thủ công trong SSMS:**
   - Right-click **Databases** → **New Database**
   - Tên: `quanlyphancong`
   - Click **OK**
   - Sau đó chạy migration: `dotnet ef database update`

## Kiểm tra SQL Server đang chạy:

```powershell
# Kiểm tra service SQL Server
Get-Service -Name "MSSQL*" | Where-Object { $_.Status -eq "Running" }

# Hoặc kiểm tra trong Services (services.msc)
# Tìm "SQL Server (SQLEXPRESS)" hoặc "SQL Server (MSSQLSERVER)"
```

## Kiểm tra kết nối:

Sau khi sửa, kiểm tra lại:
- URL: `http://localhost:8080/api/test-db`
- Hoặc: `http://172.21.40.198:8080/api/test-db`

Nếu thành công, sẽ thấy thông tin database thay vì lỗi.

## Troubleshooting:

### Lỗi: "Login failed for user"
- Kiểm tra username/password đúng chưa
- Kiểm tra SQL Server cho phép SQL Authentication chưa (Server Properties → Security)

### Lỗi: "Cannot open database"
- Kiểm tra database đã được tạo chưa
- Kiểm tra user có quyền truy cập database chưa

### Lỗi: "A network-related or instance-specific error"
- Kiểm tra SQL Server đang chạy
- Kiểm tra SQL Server Browser service đang chạy
- Kiểm tra firewall cho phép port SQL Server (1433) chưa
- Kiểm tra tên instance đúng chưa (localhost\SQLEXPRESS)

