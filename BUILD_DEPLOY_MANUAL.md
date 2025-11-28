# Hướng Dẫn Build & Deploy Thủ Công

## 📋 Tổng Quan

Hướng dẫn này sẽ giúp bạn build và deploy ứng dụng ASP.NET Core thủ công từng bước.

**Backend URL:** `http://172.20.115.40:8080/api`  
**Frontend URL:** `http://localsite.thibidi.com`

---

## 🔧 Yêu Cầu

### Trên Máy Build (Dev Machine):
- ✅ .NET 9.0 SDK (không phải Runtime)
- ✅ PowerShell 5.1+
- ✅ Quyền truy cập vào project folder

### Trên Server (Production):
- ✅ ASP.NET Core 9.0 Hosting Bundle
- ✅ IIS đã cấu hình
- ✅ Application Pool: `quanlyfilesBE`
- ✅ Quyền Administrator (để restart IIS)

---

## 📝 Bước 1: Kiểm Tra .NET SDK

Mở PowerShell và chạy:

```powershell
dotnet --version
```

**Kết quả mong đợi:** `9.0.x` hoặc cao hơn

**Nếu báo lỗi:**
- Download .NET 9.0 SDK từ: https://dotnet.microsoft.com/download/dotnet/9.0
- Chọn **SDK** (không phải Runtime)

---

## 📝 Bước 2: Chuyển Đến Thư Mục Project

```powershell
cd D:\Project\thibidi\quanlyfiles\quanlyfilesBE
```

Kiểm tra có file `quanlyfilesBE.csproj`:

```powershell
dir *.csproj
```

---

## 📝 Bước 3: Clean Build (Tùy chọn)

Xóa các file build cũ:

```powershell
dotnet clean
```

---

## 📝 Bước 4: Restore Dependencies

Tải về các packages cần thiết:

```powershell
dotnet restore
```

**Kết quả mong đợi:**
```
Restore succeeded.
```

---

## 📝 Bước 5: Build Application

Build với cấu hình Release:

```powershell
dotnet build -c Release
```

**Kết quả mong đợi:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**Nếu có lỗi:**
- Kiểm tra lỗi compilation
- Kiểm tra connection string trong `appsettings.json`
- Kiểm tra các dependencies

---

## 📝 Bước 6: Publish Application

### Option A: Publish vào thư mục tạm (Khuyến nghị)

```powershell
dotnet publish -c Release -o .\publish
```

**Kết quả:**
- Files được publish vào thư mục `.\publish`
- Có thể copy lên server sau

### Option B: Publish trực tiếp lên Server (Cần quyền Admin)

```powershell
dotnet publish -c Release -o C:\inetpub\wwwroot\quanlyfilesBE
```

**Lưu ý:** 
- Cần chạy PowerShell với quyền Administrator
- Thư mục `C:\inetpub\wwwroot\quanlyfilesBE` phải tồn tại

---

## 📝 Bước 7: Kiểm Tra Files Đã Publish

```powershell
dir .\publish
```

**Files quan trọng cần có:**
- ✅ `quanlyfilesBE.dll` (file chính)
- ✅ `web.config`
- ✅ `appsettings.json`
- ✅ `appsettings.Development.json`
- ✅ Các file `.dll` dependencies

---

## 📝 Bước 8: Copy Files Lên Server (Nếu dùng Option A)

### Cách 1: Copy qua Network Share

```powershell
# Tạo network share hoặc dùng UNC path
Copy-Item -Path .\publish\* -Destination \\172.20.115.40\C$\inetpub\wwwroot\quanlyfilesBE\ -Recurse -Force
```

### Cách 2: Copy qua Remote Desktop

1. Kết nối Remote Desktop vào server
2. Copy toàn bộ thư mục `publish` lên server
3. Paste vào `C:\inetpub\wwwroot\quanlyfilesBE`

### Cách 3: Dùng robocopy (Khuyến nghị)

```powershell
robocopy .\publish \\172.20.115.40\C$\inetpub\wwwroot\quanlyfilesBE /MIR /Z /R:3 /W:5
```

**Giải thích:**
- `/MIR`: Mirror (đồng bộ hoàn toàn)
- `/Z`: Resume support
- `/R:3`: Retry 3 lần nếu lỗi
- `/W:5`: Wait 5 giây giữa các retry

---

## 📝 Bước 9: Kiểm Tra appsettings.json trên Server

**Quan trọng:** Đảm bảo `appsettings.json` trên server có cấu hình đúng:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;..."
  },
  "Jwt": {
    "Issuer": "quanlyfiles",
    "Audience": "quanlyfiles-client",
    "Key": "...",
    "ExpiryMinutes": 120
  },
  "FileStorage": {
    "Path": "C:\\THIBIDI-STORE\\p-TK"
  }
}
```

**Lưu ý:** Không copy `appsettings.Development.json` lên production nếu không cần.

---

## 📝 Bước 10: Restart IIS Application Pool

### Trên Server, mở PowerShell với quyền Administrator:

```powershell
# Import module
Import-Module WebAdministration

# Restart Application Pool
Restart-WebAppPool -Name "quanlyfilesBE"

# Đợi 2 giây
Start-Sleep -Seconds 2

# Kiểm tra trạng thái
Get-WebAppPoolState -Name "quanlyfilesBE"
```

**Kết quả mong đợi:**
```
Value
-----
Started
```

### Hoặc dùng IIS Manager (GUI):

1. Mở **IIS Manager** (`inetmgr`)
2. Mở rộng server name
3. Click **Application Pools**
4. Tìm `quanlyfilesBE`
5. Click chuột phải → **Recycle**

---

## 📝 Bước 11: Kiểm Tra Application Đã Chạy

### Test Swagger UI:

Mở browser và truy cập:
```
http://172.20.115.40:8080/swagger
```

**Kết quả mong đợi:** Swagger UI hiển thị các API endpoints

### Test Database Connection:

```
http://172.20.115.40:8080/api/test-db
```

**Kết quả mong đợi:**
```json
{
  "success": true,
  "message": "Database connection successful",
  "provider": "Microsoft.EntityFrameworkCore.SqlServer",
  ...
}
```

---

## 📝 Bước 12: Test CORS Configuration

### Test Preflight Request (OPTIONS):

```powershell
curl -X OPTIONS http://172.20.115.40:8080/api/auth/login/firebase-token `
  -H "Origin: http://localsite.thibidi.com" `
  -H "Access-Control-Request-Method: POST" `
  -H "Access-Control-Request-Headers: Content-Type,Authorization" `
  -v
```

**Kiểm tra Response Headers phải có:**
```
Access-Control-Allow-Origin: http://localsite.thibidi.com
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS, PATCH
Access-Control-Allow-Headers: Content-Type, Authorization, X-Requested-With, Accept, Origin
Access-Control-Allow-Credentials: true
Access-Control-Expose-Headers: Authorization
```

### Test Actual Request:

```powershell
curl -X POST http://172.20.115.40:8080/api/auth/login/firebase-token `
  -H "Origin: http://localsite.thibidi.com" `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer test" `
  -d '{"idToken":"test"}' `
  -v
```

---

## 📝 Bước 13: Test từ Frontend

1. Mở frontend: `http://localsite.thibidi.com`
2. Mở Browser DevTools (F12)
3. Tab **Network**
4. Thực hiện login hoặc gọi API
5. Kiểm tra:
   - ✅ Không có CORS errors
   - ✅ Requests trả về 200 OK
   - ✅ Response headers có `Access-Control-Allow-Origin`

---

## 🔍 Troubleshooting

### Lỗi: "Cannot find .NET SDK"

**Giải pháp:**
```powershell
# Kiểm tra SDK đã cài
dotnet --list-sdks

# Nếu không có, download từ:
# https://dotnet.microsoft.com/download/dotnet/9.0
# Chọn "SDK" (không phải Runtime)
```

### Lỗi: "Access denied" khi publish

**Giải pháp:**
- Chạy PowerShell với quyền Administrator
- Hoặc cấp quyền Write cho thư mục:
```powershell
icacls C:\inetpub\wwwroot\quanlyfilesBE /grant "${env:USERNAME}:(OI)(CI)(F)"
```

### Lỗi: Application Pool không start

**Giải pháp:**
1. Kiểm tra logs:
```powershell
Get-Content C:\inetpub\wwwroot\quanlyfilesBE\logs\stdout_*.log -Tail 50
```

2. Kiểm tra Event Viewer:
   - Windows Logs → Application
   - Tìm lỗi liên quan đến `quanlyfilesBE`

3. Kiểm tra connection string trong `appsettings.json`

### Lỗi: CORS vẫn không hoạt động

**Giải pháp:**
1. Đảm bảo đã restart Application Pool
2. Kiểm tra `Program.cs` có cấu hình CORS đúng không
3. Kiểm tra Origin trong request có khớp chính xác không (không có trailing slash)
4. Kiểm tra browser console có lỗi gì không

### Lỗi: "The specified path does not exist"

**Giải pháp:**
```powershell
# Tạo thư mục
New-Item -ItemType Directory -Path C:\inetpub\wwwroot\quanlyfilesBE -Force
```

---

## 📋 Checklist Sau Khi Deploy

- [ ] Build thành công không có lỗi
- [ ] Files đã được publish đầy đủ
- [ ] `appsettings.json` trên server đúng cấu hình
- [ ] Application Pool đã restart và ở trạng thái "Started"
- [ ] Swagger UI có thể truy cập được
- [ ] Database connection test thành công
- [ ] CORS preflight request trả về đúng headers
- [ ] Frontend có thể gọi API không có CORS errors
- [ ] Authentication hoạt động đúng
- [ ] Logs không có lỗi

---

## 🚀 Quick Commands Summary

```powershell
# 1. Build
cd D:\Project\thibidi\quanlyfiles\quanlyfilesBE
dotnet restore
dotnet build -c Release

# 2. Publish
dotnet publish -c Release -o .\publish

# 3. Copy lên server (nếu cần)
robocopy .\publish \\172.20.115.40\C$\inetpub\wwwroot\quanlyfilesBE /MIR /Z

# 4. Restart IIS (trên server)
Import-Module WebAdministration
Restart-WebAppPool -Name "quanlyfilesBE"

# 5. Test
curl http://172.20.115.40:8080/api/test-db
```

---

## 📝 Lưu Ý Quan Trọng

1. **Luôn backup trước khi deploy:**
```powershell
Copy-Item C:\inetpub\wwwroot\quanlyfilesBE C:\inetpub\wwwroot\quanlyfilesBE_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss') -Recurse
```

2. **Kiểm tra CORS sau mỗi lần deploy:**
   - CORS configuration trong `Program.cs` đã được build vào DLL
   - Phải restart Application Pool để áp dụng thay đổi

3. **Nếu có lỗi, xem logs:**
```powershell
Get-Content C:\inetpub\wwwroot\quanlyfilesBE\logs\stdout_*.log -Tail 100
```

4. **Test ngay sau khi deploy:**
   - Test API endpoints
   - Test CORS từ frontend
   - Test authentication
   - Test database connection

---

## ✅ Kết Luận

Sau khi hoàn thành tất cả các bước:

1. ✅ Application đã được build và deploy thành công
2. ✅ CORS đã được cấu hình với:
   - `http://localsite.thibidi.com`
   - `http://localhost:4200`
   - Exposed Authorization header
3. ✅ Frontend có thể gọi API không có CORS errors
4. ✅ Application đang chạy ổn định trên IIS

**Nếu vẫn gặp vấn đề, kiểm tra:**
- Logs trong `C:\inetpub\wwwroot\quanlyfilesBE\logs\`
- Event Viewer → Application logs
- Browser DevTools → Network tab
- IIS Manager → Application Pool status








