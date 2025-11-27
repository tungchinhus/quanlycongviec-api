# Hướng Dẫn Build và Deploy Thủ Công

## ⚠️ Quan Trọng: SDK vs Runtime

**Sự khác biệt:**
- **.NET SDK**: Cần để **BUILD** ứng dụng (có `dotnet build`, `dotnet publish`)
- **.NET Runtime / Hosting Bundle**: Chỉ để **CHẠY** ứng dụng đã build sẵn

**2 Cách Deploy:**

### Cách 1: Build trên máy Dev, Copy lên Server (Khuyến nghị) ✅
- **Máy Dev**: Cần **.NET SDK** (download từ https://dotnet.microsoft.com/download)
- **Server**: Chỉ cần **ASP.NET Core Hosting Bundle** (bạn đang tải đúng rồi!)

### Cách 2: Build trực tiếp trên Server
- **Server**: Cần **.NET SDK** (không chỉ Runtime)

---

## Cách 1: Build trên Dev, Deploy lên Server (Khuyến nghị)

### Trên Máy Dev (có .NET SDK):

#### Bước 1: Mở PowerShell
```powershell
cd D:\Project\thibidi\quanlyfiles\quanlyfilesBE
```

#### Bước 2: Build và Publish
```powershell
# Build
dotnet build -c Release

# Publish vào thư mục tạm
dotnet publish -c Release -o .\publish
```

#### Bước 3: Copy files lên Server
- Copy toàn bộ thư mục `publish` lên server vào `C:\inetpub\wwwroot\quanlyfilesBE`
- Hoặc dùng robocopy, xcopy, hoặc network share

### Trên Server (chỉ cần Runtime):

#### Bước 1: Cài đặt ASP.NET Core Hosting Bundle
- Download và cài đặt file bạn đang tải: `dotnet-hosting-9.0.11-win.exe`
- Restart IIS sau khi cài đặt

#### Bước 2: Copy files đã build lên server
- Copy từ máy dev → `C:\inetpub\wwwroot\quanlyfilesBE`

#### Bước 3: Restart IIS Application Pool
```powershell
Import-Module WebAdministration
Restart-WebAppPool -Name "quanlyfilesBE"
```

---

## Cách 2: Build trực tiếp trên Server

**Lưu ý:** Server cần có **.NET SDK**, không chỉ Runtime!

### Bước 1: Cài đặt .NET SDK trên Server
1. Download **.NET 9.0 SDK** (không phải Runtime):
   - https://dotnet.microsoft.com/download/dotnet/9.0
   - Chọn "SDK" (không phải "Runtime")
2. Cài đặt SDK
3. Restart PowerShell và kiểm tra:
   ```powershell
   dotnet --version
   ```

### Bước 2: Mở PowerShell với quyền Administrator

1. Nhấn `Win + X`
2. Chọn "Windows PowerShell (Admin)" hoặc "Terminal (Admin)"

### Bước 3: Chuyển đến thư mục project

```powershell
cd D:\Project\thibidi\quanlyfiles\quanlyfilesBE
```

**Hoặc nếu project ở server:**
```powershell
cd C:\inetpub\wwwroot\quanlyfilesBE
```

### Bước 4: Kiểm tra .NET SDK

```powershell
dotnet --version
```

**Nếu báo lỗi "No .NET SDKs were found":**
- Bạn đang dùng **Runtime** thay vì **SDK**
- Cần download và cài **.NET 9.0 SDK** từ: https://dotnet.microsoft.com/download/dotnet/9.0
- Chọn "SDK" (không phải "Runtime" hay "Hosting Bundle")

### Bước 5: Build Application

```powershell
dotnet build -c Release
```

Kết quả mong đợi:
- Build thành công
- Không có lỗi compilation

### Bước 6: Publish Application

```powershell
dotnet publish -c Release -o C:\inetpub\wwwroot\quanlyfilesBE
```

**Lưu ý:** 
- Thay đổi đường dẫn `C:\inetpub\wwwroot\quanlyfilesBE` nếu bạn deploy ở vị trí khác
- Quá trình publish sẽ mất vài phút

### Bước 7: Kiểm tra files đã được publish

```powershell
dir C:\inetpub\wwwroot\quanlyfilesBE
```

Bạn sẽ thấy:
- `quanlyfilesBE.dll` (file chính)
- `web.config`
- Các file dependencies khác

### Bước 8: Restart IIS Application Pool

### Cách 1: Dùng PowerShell

```powershell
Import-Module WebAdministration
Restart-WebAppPool -Name "quanlyfilesBE"
```

**Lưu ý:** Thay `"quanlyfilesBE"` bằng tên Application Pool thực tế của bạn nếu khác.

### Cách 2: Dùng IIS Manager (GUI)

1. Mở **IIS Manager** (inetmgr)
2. Mở rộng server name
3. Click vào **Application Pools**
4. Tìm Application Pool `quanlyfilesBE`
5. Click chuột phải → **Recycle** hoặc **Stop** rồi **Start**

### Bước 9: Kiểm tra Application đã chạy

### Kiểm tra Application Pool status:

```powershell
Import-Module WebAdministration
Get-WebAppPoolState -Name "quanlyfilesBE"
```

Kết quả mong đợi: `Started`

### Test API endpoint:

Mở browser và truy cập:
- `http://172.20.115.40:8080/swagger` (Swagger UI)
- `http://172.20.115.40:8080/api/test-db` (Test database)

### Bước 10: Kiểm tra CORS đã được cập nhật

Sau khi deploy, CORS đã được cập nhật trong `Program.cs` với:
- ✅ `http://localsite.thibidi.com`
- ✅ `http://localhost:4200`
- ✅ Exposed Authorization header

### Test CORS với curl:

```powershell
# Test preflight request
curl -X OPTIONS http://172.20.115.40:8080/api/auth/login/firebase-token `
  -H "Origin: http://localsite.thibidi.com" `
  -H "Access-Control-Request-Method: POST" `
  -H "Access-Control-Request-Headers: Content-Type,Authorization" `
  -v
```

Kiểm tra response headers phải có:
- `Access-Control-Allow-Origin: http://localsite.thibidi.com`
- `Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS`
- `Access-Control-Allow-Headers: Content-Type, Authorization`
- `Access-Control-Allow-Credentials: true`

---

## Troubleshooting

### Lỗi: "Cannot find .NET SDK"

**Giải pháp:**
```powershell
# Kiểm tra .NET SDK đã cài đặt
dotnet --list-sdks

# Nếu không có, download và cài đặt từ:
# https://dotnet.microsoft.com/download
```

### Lỗi: "Access denied" khi publish

**Giải pháp:**
- Chạy PowerShell với quyền Administrator
- Hoặc cấp quyền Write cho thư mục publish:
```powershell
icacls C:\inetpub\wwwroot\quanlyfilesBE /grant "${env:USERNAME}:(OI)(CI)(F)"
```

### Lỗi: Application Pool không start

**Giải pháp:**
1. Kiểm tra logs tại: `C:\inetpub\wwwroot\quanlyfilesBE\logs\stdout_*.log`
2. Kiểm tra Event Viewer → Windows Logs → Application
3. Kiểm tra connection string trong `appsettings.json`

### Lỗi: "The specified path does not exist"

**Giải pháp:**
- Tạo thư mục trước:
```powershell
New-Item -ItemType Directory -Path C:\inetpub\wwwroot\quanlyfilesBE -Force
```

---

## Quick Commands Summary

```powershell
# 1. Build
cd D:\Project\thibidi\quanlyfiles\quanlyfilesBE
dotnet build -c Release

# 2. Publish
dotnet publish -c Release -o C:\inetpub\wwwroot\quanlyfilesBE

# 3. Restart IIS
Import-Module WebAdministration
Restart-WebAppPool -Name "quanlyfilesBE"

# 4. Check status
Get-WebAppPoolState -Name "quanlyfilesBE"
```

---

## Lưu ý Quan Trọng

1. **Luôn backup trước khi deploy:**
   ```powershell
   Copy-Item C:\inetpub\wwwroot\quanlyfilesBE C:\inetpub\wwwroot\quanlyfilesBE_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss') -Recurse
   ```

2. **Kiểm tra appsettings.json sau khi publish:**
   - Đảm bảo connection string đúng
   - Đảm bảo JWT settings đúng
   - Đảm bảo FileStorage path đúng

3. **Sau khi deploy, test ngay:**
   - Test API endpoints
   - Test CORS từ frontend
   - Test authentication
   - Test database connection

4. **Nếu có lỗi, xem logs:**
   ```powershell
   Get-Content C:\inetpub\wwwroot\quanlyfilesBE\logs\stdout_*.log -Tail 50
   ```

