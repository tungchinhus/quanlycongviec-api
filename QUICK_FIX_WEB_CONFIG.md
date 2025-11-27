# Hướng Dẫn Nhanh Sửa Lỗi 500.19 - web.config

## Cách 1: Sử dụng Script (Khuyến nghị)

Mở PowerShell và chạy:

```powershell
cd D:\Project\thibidi\quanlyfiles\quanlyfilesBE
.\create-webconfig.ps1
```

Script này sẽ:
- Xóa file web.config cũ (nếu có)
- Tạo file web.config mới với encoding đúng (UTF-8 no BOM)
- Kiểm tra cú pháp XML
- Xác nhận cấu hình hợp lệ

## Cách 2: Copy File Thủ Công

1. **Copy file web.config từ project root:**
   ```powershell
   Copy-Item "D:\Project\thibidi\quanlyfiles\quanlyfilesBE\web.config" -Destination "C:\inetpub\wwwroot\quanlyfilesBE\web.config" -Force
   ```

2. **Hoặc tạo file mới bằng Notepad:**
   - Mở Notepad
   - Copy nội dung dưới đây
   - Lưu với tên `web.config` vào `C:\inetpub\wwwroot\quanlyfilesBE\`
   - **Quan trọng:** Khi lưu, chọn "Encoding: UTF-8" (không phải UTF-8 with BOM)

## Nội dung web.config

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

## Sau khi sửa:

1. **Mở IIS Manager**
2. **Restart Application Pool:**
   - Right-click Application Pool `quanlyfilesBE`
   - Chọn **Recycle** hoặc **Stop** rồi **Start**
3. **Refresh website** trong browser

## Kiểm tra file web.config:

```powershell
# Kiểm tra file có tồn tại
Test-Path "C:\inetpub\wwwroot\quanlyfilesBE\web.config"

# Kiểm tra cú pháp XML
[xml]$xml = Get-Content "C:\inetpub\wwwroot\quanlyfilesBE\web.config"
```

## Nếu vẫn còn lỗi:

1. **Kiểm tra .NET 9.0 Hosting Bundle đã được cài đặt:**
   - Tải từ: https://dotnet.microsoft.com/download/dotnet/9.0
   - Chọn "Hosting Bundle"
   - Sau khi cài, chạy: `iisreset`

2. **Kiểm tra Application Pool:**
   - .NET CLR Version phải là "No Managed Code"
   - Managed Pipeline Mode phải là "Integrated"

3. **Kiểm tra quyền truy cập:**
   ```powershell
   .\fix-iis-permissions.ps1
   ```

4. **Xem Event Viewer để biết lỗi chi tiết:**
   - Mở Event Viewer
   - Windows Logs → Application
   - Tìm lỗi liên quan đến IIS hoặc ASP.NET Core

