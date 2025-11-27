# Hướng Dẫn Deploy .NET 9.0 lên IIS

## Yêu Cầu Hệ Thống

### 1. Trên Server Windows
- Windows Server 2016 trở lên hoặc Windows 10/11
- IIS 10.0 trở lên
- .NET 9.0 Hosting Bundle (ASP.NET Core Runtime + .NET Runtime)
- SQL Server (nếu dùng SQL Server)

### 2. Cài Đặt .NET 9.0 Hosting Bundle

1. Tải .NET 9.0 Hosting Bundle từ: https://dotnet.microsoft.com/download/dotnet/9.0
2. Chạy file cài đặt `dotnet-hosting-9.0.x-win.exe`
3. Khởi động lại IIS sau khi cài đặt:
   ```powershell
   iisreset
   ```

### 3. Cài Đặt IIS Modules Cần Thiết

Mở PowerShell với quyền Administrator và chạy:

```powershell
# Cài đặt IIS và các module cần thiết
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors
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

## Bước 1: Publish Application

### Cách 1: Sử dụng Visual Studio
1. Right-click vào project `quanlyfilesBE`
2. Chọn **Publish**
3. Chọn **Folder** hoặc **IIS**
4. Chọn thư mục publish (ví dụ: `C:\inetpub\wwwroot\quanlyfilesBE`)
5. Click **Publish**

### Cách 2: Sử dụng Command Line

```powershell
# Publish với Release configuration
dotnet publish -c Release -o "C:\inetpub\wwwroot\quanlyfilesBE"
```

### Cách 3: Sử dụng Script PowerShell

Chạy script `publish-to-iis.ps1` (xem file script bên dưới)

## Bước 2: Cấu Hình IIS

### 2.1. Tạo Application Pool

1. Mở **IIS Manager** (inetmgr)
2. Right-click **Application Pools** → **Add Application Pool**
3. Đặt tên: `quanlyfilesBE`
4. .NET CLR Version: **No Managed Code** (quan trọng!)
5. Managed Pipeline Mode: **Integrated**
6. Click **OK**

### 2.2. Cấu Hình Application Pool

1. Right-click Application Pool `quanlyfilesBE` → **Advanced Settings**
2. Thiết lập:
   - **Start Mode**: `AlwaysRunning`
   - **Identity**: `ApplicationPoolIdentity` (hoặc custom account nếu cần)
   - **Idle Timeout**: `0` (nếu muốn app luôn chạy)
   - **Regular Time Interval**: `0` (tắt recycle)

### 2.3. Tạo Website/Application

1. Right-click **Sites** → **Add Website**
2. Điền thông tin:
   - **Site name**: `quanlyfilesBE`
   - **Application pool**: `quanlyfilesBE`
   - **Physical path**: `C:\inetpub\wwwroot\quanlyfilesBE`
   - **Binding**:
     - Type: `http` hoặc `https`
     - IP address: `All Unassigned` hoặc IP cụ thể
     - Port: `80` (http) hoặc `443` (https)
     - Host name: (để trống hoặc nhập domain)
3. Click **OK**

## Bước 3: Cấu Hình web.config

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
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

## Bước 4: Cấu Hình appsettings.json

Cập nhật `appsettings.json` hoặc `appsettings.Production.json` trong thư mục publish:

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

## Bước 5: Cấu Hình Quyền Truy Cập

### 5.1. Quyền cho Application Pool Identity

```powershell
# Cấp quyền đọc/ghi cho Application Pool Identity
$appPoolName = "quanlyfilesBE"
$appPath = "C:\inetpub\wwwroot\quanlyfilesBE"
$logsPath = "$appPath\logs"

# Tạo thư mục logs nếu chưa có
New-Item -ItemType Directory -Force -Path $logsPath

# Cấp quyền
icacls $appPath /grant "IIS AppPool\$appPoolName:(OI)(CI)(RX)" /T
icacls $logsPath /grant "IIS AppPool\$appPoolName:(OI)(CI)(F)" /T

# Nếu có thư mục file storage
$storagePath = "C:\THIBIDI-STORE\p-TK"
if (Test-Path $storagePath) {
    icacls $storagePath /grant "IIS AppPool\$appPoolName:(OI)(CI)(F)" /T
}
```

### 5.2. Quyền cho SQL Server

Đảm bảo Application Pool Identity hoặc service account có quyền truy cập database.

## Bước 6: Cấu Hình CORS (Nếu Cần)

Nếu frontend chạy trên domain khác, cập nhật CORS trong `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("https://yourdomain.com", "http://yourdomain.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});
```

## Bước 7: Kiểm Tra và Khởi Động

1. Mở IIS Manager
2. Chọn website `quanlyfilesBE`
3. Click **Browse Website** hoặc truy cập URL: `http://localhost` hoặc `http://your-domain`
4. Kiểm tra logs nếu có lỗi: `C:\inetpub\wwwroot\quanlyfilesBE\logs\stdout_*.log`

## Bước 8: Cấu Hình HTTPS (Tùy Chọn)

### 8.1. Tạo SSL Certificate

```powershell
# Tạo self-signed certificate (chỉ dùng cho test)
New-SelfSignedCertificate -DnsName "yourdomain.com" -CertStoreLocation "cert:\LocalMachine\My"
```

### 8.2. Bind HTTPS trong IIS

1. Right-click website → **Edit Bindings**
2. Click **Add**
3. Type: `https`
4. Port: `443`
5. SSL certificate: Chọn certificate vừa tạo
6. Click **OK**

## Troubleshooting

### Lỗi Authorization - "Cannot verify access to path"

**Triệu chứng:**
- Trong IIS Manager, khi chạy "Test Connection" thấy:
  - ✅ Authentication: Passed
  - ⚠️ Authorization: Failed - "Cannot verify access to path"

**Nguyên nhân:**
Application Pool Identity không có quyền truy cập vào thư mục ứng dụng.

**Giải pháp:**

**Cách 1: Sử dụng Script (Khuyến nghị)**
```powershell
# Chạy PowerShell với quyền Administrator
.\fix-iis-permissions.ps1
```

**Cách 2: Thủ công bằng PowerShell**
```powershell
# Mở PowerShell với quyền Administrator
$appPoolName = "quanlyfilesBE"
$appPath = "C:\inetpub\wwwroot\quanlyfilesBE"
$appPoolIdentity = "IIS AppPool\$appPoolName"

# Cấp quyền đầy đủ cho thư mục ứng dụng
icacls $appPath /grant "${appPoolIdentity}:(OI)(CI)(F)" /T

# Cấp quyền cho thư mục logs
$logsPath = "$appPath\logs"
if (-not (Test-Path $logsPath)) {
    New-Item -ItemType Directory -Path $logsPath -Force
}
icacls $logsPath /grant "${appPoolIdentity}:(OI)(CI)(F)" /T

# Cấp quyền cho thư mục file storage (nếu có)
$storagePath = "C:\THIBIDI-STORE\p-TK"
if (Test-Path $storagePath) {
    icacls $storagePath /grant "${appPoolIdentity}:(OI)(CI)(F)" /T
}
```

**Cách 3: Thủ công bằng Windows Explorer**
1. Right-click vào thư mục `C:\inetpub\wwwroot\quanlyfilesBE`
2. Chọn **Properties** → Tab **Security**
3. Click **Edit** → **Add**
4. Nhập: `IIS AppPool\quanlyfilesBE`
5. Click **Check Names** để verify
6. Click **OK**
7. Chọn **Full control** hoặc ít nhất **Read & execute**, **List folder contents**, **Read**, **Write**
8. Click **OK** → **Apply** → **OK**
9. Lặp lại cho thư mục `logs` và thư mục file storage

**Sau khi cấp quyền:**
1. Restart Application Pool trong IIS Manager
2. Chạy lại "Test Connection" trong IIS Manager
3. Kiểm tra website có hoạt động không

### Lỗi 500.19 - Invalid Configuration Data (Error Code: 0x8007000d)

**Triệu chứng:**
- HTTP Error 500.19 - Internal Server Error
- Error Code: `0x8007000d` (ERROR_INVALID_DATA)
- Config File: `C:\inetpub\wwwroot\quanlyfilesBE\web.config`
- Config Error và Config Source để trống

**Nguyên nhân:**
- File `web.config` có lỗi cú pháp XML
- File `web.config` bị thiếu hoặc có cấu hình không hợp lệ
- ASP.NET Core Module chưa được cài đặt

**Giải pháp:**

**Cách 1: Sử dụng Script (Khuyến nghị)**
```powershell
# Chạy script để sửa web.config
.\fix-webconfig.ps1
```

**Cách 2: Kiểm tra và sửa thủ công**

1. **Kiểm tra ASP.NET Core Hosting Bundle đã được cài đặt:**
   ```powershell
   # Kiểm tra module đã được cài đặt
   Get-WindowsFeature | Where-Object {$_.Name -like "*AspNetCore*"}
   ```
   Nếu chưa có, tải và cài đặt từ: https://dotnet.microsoft.com/download/dotnet/9.0

2. **Kiểm tra file web.config:**
   - Mở file `C:\inetpub\wwwroot\quanlyfilesBE\web.config`
   - Đảm bảo cú pháp XML đúng (không có thẻ trùng lặp, thẻ đóng đầy đủ)
   - Đảm bảo có đầy đủ các thẻ: `<handlers>`, `<aspNetCore>`

3. **Tạo lại web.config nếu cần:**
   - Xóa file web.config cũ
   - Copy file `web.config` từ project root vào thư mục publish
   - Hoặc tạo mới theo mẫu trong hướng dẫn

4. **Kiểm tra encoding:**
   - File phải có encoding UTF-8
   - Không có BOM (Byte Order Mark)

**Sau khi sửa:**
1. Restart Application Pool trong IIS Manager
2. Refresh website và kiểm tra lại

### Lỗi 500.30 - In-Process Start Failure

- Kiểm tra .NET 9.0 Hosting Bundle đã được cài đặt
- Kiểm tra Application Pool đang dùng .NET CLR Version = "No Managed Code"
- Kiểm tra logs trong `logs\stdout_*.log`

### Lỗi 500.0 - ANCM In-Process Handler Load Failure

- Kiểm tra file `quanlyfilesBE.dll` có trong thư mục publish
- Kiểm tra quyền truy cập thư mục
- Kiểm tra `web.config` có đúng cấu hình

### Lỗi Kết Nối Database

- Kiểm tra connection string trong `appsettings.json`
- Kiểm tra SQL Server đang chạy
- Kiểm tra firewall cho phép kết nối SQL Server
- Kiểm tra quyền user database

### Lỗi File Upload

- Kiểm tra quyền ghi vào thư mục `FileStorage.Path`
- Kiểm tra giới hạn kích thước file trong `Program.cs` và IIS

### Kiểm Tra Logs

```powershell
# Xem logs của ứng dụng
Get-Content "C:\inetpub\wwwroot\quanlyfilesBE\logs\stdout_*.log" -Tail 50

# Xem Event Viewer
Get-EventLog -LogName Application -Source "IIS*" -Newest 20
```

## Script Tự Động Hóa

Xem file `deploy-to-iis.ps1` để tự động hóa quá trình deploy.

## Tài Liệu Tham Khảo

- [Host ASP.NET Core on Windows with IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/)
- [.NET 9.0 Download](https://dotnet.microsoft.com/download/dotnet/9.0)

