# 🚀 Hướng Dẫn Build và Deploy lên IIS

Hướng dẫn chi tiết để build và deploy ứng dụng QuanLyFiles Backend (.NET 9.0) lên IIS.

---

## 📋 Mục Lục

1. [Yêu Cầu Hệ Thống](#yêu-cầu-hệ-thống)
2. [Cài Đặt Cần Thiết](#cài-đặt-cần-thiết)
3. [Build và Deploy Tự Động (Khuyến nghị)](#build-và-deploy-tự-động-khuyến-nghị)
4. [Build và Deploy Thủ Công](#build-và-deploy-thủ-công)
5. [Kiểm Tra Sau Khi Deploy](#kiểm-tra-sau-khi-deploy)
6. [Troubleshooting](#troubleshooting)

---

## 🖥️ Yêu Cầu Hệ Thống

### Trên Máy Build (Development Machine):
- ✅ Windows 10/11 hoặc Windows Server 2016+
- ✅ .NET 9.0 SDK (không phải Runtime)
- ✅ PowerShell 5.1+
- ✅ Quyền truy cập vào project folder

### Trên Server IIS (Production):
- ✅ Windows Server 2016+ hoặc Windows 10/11
- ✅ IIS 10.0+
- ✅ ASP.NET Core 9.0 Hosting Bundle
- ✅ SQL Server (nếu dùng SQL Server)
- ✅ Quyền Administrator

---

## 🔧 Cài Đặt Cần Thiết

### 1. Cài Đặt .NET 9.0 SDK (Máy Build)

1. Tải .NET 9.0 SDK từ: https://dotnet.microsoft.com/download/dotnet/9.0
2. Chọn **SDK** (không phải Runtime)
3. Cài đặt và khởi động lại máy
4. Kiểm tra:
   ```powershell
   dotnet --version
   ```
   Kết quả mong đợi: `9.0.x`

### 2. Cài Đặt ASP.NET Core 9.0 Hosting Bundle (Server IIS)

1. Tải .NET 9.0 Hosting Bundle từ: https://dotnet.microsoft.com/download/dotnet/9.0
2. Chọn **Hosting Bundle** (ASP.NET Core Runtime + .NET Runtime)
3. Chạy file cài đặt `dotnet-hosting-9.0.x-win.exe`
4. Khởi động lại IIS:
   ```powershell
   iisreset
   ```

### 3. Cài Đặt IIS Modules (Nếu Chưa Có)

Mở PowerShell với quyền Administrator và chạy:

```powershell
# Cài đặt IIS và các module cần thiết
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ApplicationInit
Enable-WindowsOptionalFeature -Online -FeatureName IIS-NetFxExtensibility45
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HealthAndDiagnostics
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging
Enable-WindowsOptionalFeature -Online -FeatureName IIS-Security
Enable-WindowsOptionalFeature -Online -FeatureName IIS-RequestFiltering
Enable-WindowsOptionalFeature -Online -FeatureName IIS-Performance
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpCompressionStatic
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerManagementTools
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementConsole
```

---

## ⚡ Build và Deploy Tự Động (Khuyến nghị)

### Cách 1: Sử dụng Script PowerShell (Dễ nhất)

1. **Mở PowerShell với quyền Administrator**
   - Right-click PowerShell → "Run as Administrator"

2. **Chuyển đến thư mục project:**
   ```powershell
   cd D:\Project\thibidi\quanlyfiles\quanlyfilesBE
   ```

3. **Chạy script:**
   ```powershell
   .\build-deploy-iis.ps1
   ```

4. **Hoặc với tham số tùy chỉnh:**
   ```powershell
   .\build-deploy-iis.ps1 -PublishPath "C:\inetpub\wwwroot\quanlyfilesBE" -Port 8080
   ```

**Script sẽ tự động:**
- ✅ Clean project
- ✅ Restore dependencies
- ✅ Build với Release configuration
- ✅ Publish vào thư mục IIS
- ✅ Tạo/cập nhật web.config
- ✅ Tạo thư mục logs
- ✅ Tạo/cập nhật Application Pool
- ✅ Tạo/cập nhật Website
- ✅ Cấp quyền truy cập
- ✅ Restart Application Pool

### Các Tham Số Script:

```powershell
.\build-deploy-iis.ps1 `
    -PublishPath "C:\inetpub\wwwroot\quanlyfilesBE" `  # Đường dẫn publish
    -AppPoolName "quanlyfilesBE" `                      # Tên Application Pool
    -SiteName "quanlyfilesBE" `                         # Tên Website
    -Port 8080 `                                        # Port (mặc định: 8080)
    -Environment "Production" `                         # Environment
    -SkipBuild `                                        # Bỏ qua build (chỉ deploy)
    -SkipDeploy `                                       # Bỏ qua deploy (chỉ build)
```

### Ví Dụ Sử Dụng:

```powershell
# Build và deploy đầy đủ
.\build-deploy-iis.ps1

# Chỉ build, không deploy
.\build-deploy-iis.ps1 -SkipDeploy

# Chỉ deploy, không build (dùng khi đã build trước đó)
.\build-deploy-iis.ps1 -SkipBuild

# Deploy với port khác
.\build-deploy-iis.ps1 -Port 80

# Deploy vào thư mục khác
.\build-deploy-iis.ps1 -PublishPath "D:\WebApps\quanlyfilesBE"
```

---

## 🔨 Build và Deploy Thủ Công

Nếu muốn thực hiện từng bước thủ công:

### Bước 1: Kiểm Tra .NET SDK

```powershell
dotnet --version
```

Kết quả mong đợi: `9.0.x`

### Bước 2: Chuyển Đến Thư Mục Project

```powershell
cd D:\Project\thibidi\quanlyfiles\quanlyfilesBE
```

### Bước 3: Clean Project (Tùy chọn)

```powershell
dotnet clean -c Release
```

### Bước 4: Restore Dependencies

```powershell
dotnet restore
```

### Bước 5: Build Application

```powershell
dotnet build -c Release
```

Kết quả mong đợi:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Bước 6: Publish Application

```powershell
dotnet publish -c Release -o C:\inetpub\wwwroot\quanlyfilesBE
```

**Lưu ý:** 
- Cần chạy PowerShell với quyền Administrator
- Thư mục `C:\inetpub\wwwroot\quanlyfilesBE` phải tồn tại hoặc sẽ được tạo tự động

### Bước 7: Kiểm Tra Files Đã Publish

```powershell
dir C:\inetpub\wwwroot\quanlyfilesBE
```

**Files quan trọng cần có:**
- ✅ `quanlyfilesBE.dll` (file chính)
- ✅ `web.config`
- ✅ `appsettings.json`
- ✅ Các file `.dll` dependencies

### Bước 8: Cấu Hình web.config

File `web.config` sẽ được tự động tạo khi publish. Nếu chưa có, tạo file `web.config` trong thư mục publish:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" 
                  arguments=".\quanlyfilesBE.dll" 
                  stdoutLogEnabled="true" 
                  stdoutLogFile=".\logs\stdout" 
                  hostingModel="inprocess"
                  requestTimeout="00:20:00">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
      <httpErrors errorMode="Detailed" />
    </system.webServer>
  </location>
</configuration>
```

### Bước 9: Tạo Thư Mục Logs

```powershell
New-Item -ItemType Directory -Path "C:\inetpub\wwwroot\quanlyfilesBE\logs" -Force
```

### Bước 10: Cấu Hình IIS

#### 10.1. Tạo Application Pool

1. Mở **IIS Manager** (`inetmgr`)
2. Right-click **Application Pools** → **Add Application Pool**
3. Đặt tên: `quanlyfilesBE`
4. .NET CLR Version: **No Managed Code** (quan trọng!)
5. Managed Pipeline Mode: **Integrated**
6. Click **OK**

#### 10.2. Cấu Hình Application Pool

1. Right-click Application Pool `quanlyfilesBE` → **Advanced Settings**
2. Thiết lập:
   - **Start Mode**: `AlwaysRunning`
   - **Identity**: `ApplicationPoolIdentity`
   - **Idle Timeout**: `0` (nếu muốn app luôn chạy)
   - **Regular Time Interval**: `0` (tắt recycle)

#### 10.3. Tạo Website

1. Right-click **Sites** → **Add Website**
2. Điền thông tin:
   - **Site name**: `quanlyfilesBE`
   - **Application pool**: `quanlyfilesBE`
   - **Physical path**: `C:\inetpub\wwwroot\quanlyfilesBE`
   - **Binding**:
     - Type: `http`
     - IP address: `All Unassigned`
     - Port: `8080` (hoặc port bạn muốn)
     - Host name: (để trống hoặc nhập domain)
3. Click **OK**

### Bước 11: Cấp Quyền Truy Cập

Mở PowerShell với quyền Administrator:

```powershell
$appPoolName = "quanlyfilesBE"
$appPath = "C:\inetpub\wwwroot\quanlyfilesBE"
$logsPath = "$appPath\logs"
$appPoolIdentity = "IIS AppPool\$appPoolName"

# Quyền đọc cho thư mục ứng dụng
icacls $appPath /grant "${appPoolIdentity}:(OI)(CI)(RX)" /T

# Quyền ghi cho thư mục logs
icacls $logsPath /grant "${appPoolIdentity}:(OI)(CI)(F)" /T

# Quyền cho thư mục file storage (nếu có)
$storagePath = "C:\THIBIDI-STORE\p-TK"
if (Test-Path $storagePath) {
    icacls $storagePath /grant "${appPoolIdentity}:(OI)(CI)(F)" /T
}
```

### Bước 12: Cấu Hình appsettings.json

Cập nhật `appsettings.json` trong thư mục publish:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=quanlyphancong;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Issuer": "quanlyfiles",
    "Audience": "quanlyfiles-client",
    "Key": "YOUR_PRODUCTION_SECRET_KEY_MIN_32_CHARACTERS",
    "ExpiryMinutes": 120
  },
  "Firebase": {
    "CredentialsPath": "C:\\inetpub\\wwwroot\\quanlyfilesBE\\service-account-key.json",
    "CredentialsJson": ""
  },
  "FileStorage": {
    "Path": "C:\\THIBIDI-STORE\\p-TK"
  }
}
```

### Bước 13: Restart Application Pool

```powershell
Import-Module WebAdministration
Restart-WebAppPool -Name "quanlyfilesBE"
Start-Sleep -Seconds 2
Get-WebAppPoolState -Name "quanlyfilesBE"
```

Kết quả mong đợi:
```
Value
-----
Started
```

---

## ✅ Kiểm Tra Sau Khi Deploy

### 1. Kiểm Tra Application Pool

```powershell
Get-WebAppPoolState -Name "quanlyfilesBE"
```

Kết quả mong đợi: `Started`

### 2. Kiểm Tra Website

Mở browser và truy cập:
- **Swagger UI**: `http://localhost:8080/swagger`
- **API Test**: `http://localhost:8080/api/test-db`

### 3. Kiểm Tra Database Connection

```powershell
curl http://localhost:8080/api/test-db
```

Kết quả mong đợi:
```json
{
  "success": true,
  "message": "Database connection successful",
  "provider": "Microsoft.EntityFrameworkCore.SqlServer",
  ...
}
```

### 4. Kiểm Tra Logs

```powershell
Get-Content "C:\inetpub\wwwroot\quanlyfilesBE\logs\stdout_*.log" -Tail 50
```

### 5. Kiểm Tra CORS (Nếu có Frontend)

Từ browser console hoặc Postman, test CORS:
```javascript
fetch('http://localhost:8080/api/test-db', {
  method: 'GET',
  headers: {
    'Origin': 'http://localhost:4200'
  }
})
```

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

### Lỗi 500.19 - Invalid Configuration Data

**Triệu chứng:**
- HTTP Error 500.19 - Internal Server Error
- Error Code: `0x8007000d`

**Giải pháp:**
1. Kiểm tra ASP.NET Core Hosting Bundle đã được cài đặt
2. Kiểm tra file `web.config` có cú pháp XML đúng không
3. Kiểm tra encoding file phải là UTF-8

### Lỗi 500.30 - In-Process Start Failure

**Giải pháp:**
- Kiểm tra .NET 9.0 Hosting Bundle đã được cài đặt
- Kiểm tra Application Pool đang dùng .NET CLR Version = "No Managed Code"
- Kiểm tra logs trong `logs\stdout_*.log`

### Lỗi: Authorization - "Cannot verify access to path"

**Giải pháp:**
```powershell
$appPoolName = "quanlyfilesBE"
$appPath = "C:\inetpub\wwwroot\quanlyfilesBE"
$appPoolIdentity = "IIS AppPool\$appPoolName"

# Cấp quyền đầy đủ
icacls $appPath /grant "${appPoolIdentity}:(OI)(CI)(F)" /T

# Restart Application Pool
Restart-WebAppPool -Name $appPoolName
```

### Lỗi: Kết Nối Database

**Giải pháp:**
- Kiểm tra connection string trong `appsettings.json`
- Kiểm tra SQL Server đang chạy
- Kiểm tra firewall cho phép kết nối SQL Server
- Kiểm tra quyền user database

### Lỗi: File Upload

**Giải pháp:**
- Kiểm tra quyền ghi vào thư mục `FileStorage.Path`
- Kiểm tra giới hạn kích thước file trong `Program.cs` và IIS

---

## 📋 Checklist Sau Khi Deploy

- [ ] Build thành công không có lỗi
- [ ] Files đã được publish đầy đủ
- [ ] `appsettings.json` trên server đúng cấu hình
- [ ] `web.config` đã được tạo/cập nhật
- [ ] Application Pool đã restart và ở trạng thái "Started"
- [ ] Website có thể truy cập được
- [ ] Swagger UI có thể truy cập được
- [ ] Database connection test thành công
- [ ] Logs không có lỗi
- [ ] CORS hoạt động đúng (nếu có frontend)
- [ ] Authentication hoạt động đúng

---

## 🚀 Quick Commands Summary

```powershell
# 1. Build và Deploy tự động (Khuyến nghị)
cd D:\Project\thibidi\quanlyfiles\quanlyfilesBE
.\build-deploy-iis.ps1

# 2. Build thủ công
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o C:\inetpub\wwwroot\quanlyfilesBE

# 3. Restart IIS (trên server)
Import-Module WebAdministration
Restart-WebAppPool -Name "quanlyfilesBE"

# 4. Test
curl http://localhost:8080/api/test-db

# 5. Xem logs
Get-Content C:\inetpub\wwwroot\quanlyfilesBE\logs\stdout_*.log -Tail 50
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
2. ✅ Application đang chạy ổn định trên IIS
3. ✅ CORS đã được cấu hình đúng
4. ✅ Frontend có thể gọi API không có CORS errors

**Nếu vẫn gặp vấn đề, kiểm tra:**
- Logs trong `C:\inetpub\wwwroot\quanlyfilesBE\logs\`
- Event Viewer → Application logs
- Browser DevTools → Network tab
- IIS Manager → Application Pool status

---

**Tài Liệu Tham Khảo:**
- [Host ASP.NET Core on Windows with IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/)
- [.NET 9.0 Download](https://dotnet.microsoft.com/download/dotnet/9.0)

