# ⚡ Quick Deploy Guide

Hướng dẫn nhanh để build và deploy lên IIS.

## 🚀 Cách Nhanh Nhất

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

**Xong!** Script sẽ tự động:
- ✅ Build project
- ✅ Publish lên IIS
- ✅ Cấu hình IIS
- ✅ Restart Application Pool

## 📝 Các Tùy Chọn

```powershell
# Deploy với port khác
.\build-deploy-iis.ps1 -Port 80

# Deploy vào thư mục khác
.\build-deploy-iis.ps1 -PublishPath "D:\WebApps\quanlyfilesBE"

# Chỉ build, không deploy
.\build-deploy-iis.ps1 -SkipDeploy

# Chỉ deploy, không build (dùng khi đã build trước đó)
.\build-deploy-iis.ps1 -SkipBuild
```

## ✅ Kiểm Tra Sau Khi Deploy

```powershell
# Test API
curl http://localhost:8080/api/test-db

# Xem Swagger
# Mở browser: http://localhost:8080/swagger
```

## 📖 Hướng Dẫn Chi Tiết

Xem file `HUONG_DAN_DEPLOY_IIS.md` để biết chi tiết đầy đủ.

## 🔧 Troubleshooting

Nếu gặp lỗi, xem logs:
```powershell
Get-Content C:\inetpub\wwwroot\quanlyfilesBE\logs\stdout_*.log -Tail 50
```

Xem thêm trong `HUONG_DAN_DEPLOY_IIS.md` → Troubleshooting section.




