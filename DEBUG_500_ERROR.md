# Hướng Dẫn Debug Lỗi 500

## Tình Huống Hiện Tại

- ✅ **Preflight (OPTIONS) thành công** → CORS đã hoạt động
- ❌ **Actual requests trả về 500** → Lỗi server-side

## Các Bước Debug

### Bước 1: Kiểm tra Backend đã được Deploy với CORS mới chưa

**Kiểm tra trên server:**

```powershell
# Kiểm tra file Program.cs đã có CORS config mới
Get-Content C:\inetpub\wwwroot\quanlyfilesBE\quanlyfilesBE.dll.config
# Hoặc kiểm tra thời gian build
Get-Item C:\inetpub\wwwroot\quanlyfilesBE\quanlyfilesBE.dll | Select-Object LastWriteTime
```

**Nếu chưa deploy:**
- Build và deploy lại với CORS config mới
- Xem hướng dẫn trong `MANUAL_BUILD_DEPLOY.md`

### Bước 2: Kiểm tra Logs trên Server

**Xem logs IIS:**

```powershell
# Xem logs stdout (nếu có)
Get-Content C:\inetpub\wwwroot\quanlyfilesBE\logs\stdout_*.log -Tail 50

# Hoặc xem Event Viewer
eventvwr.msc
# Windows Logs → Application → Tìm lỗi từ quanlyfilesBE
```

**Xem logs trong browser:**

1. Mở DevTools (F12)
2. Tab **Network**
3. Click vào request bị lỗi (500)
4. Tab **Response** → Xem chi tiết error message

### Bước 3: Test API trực tiếp từ Server

**Test từ PowerShell trên server:**

```powershell
# Test firebase-token endpoint
$body = @{
    idToken = "test-token"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8080/api/auth/login/firebase-token" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body `
    -ErrorAction Stop
```

**Hoặc dùng curl:**

```powershell
curl -X POST http://localhost:8080/api/auth/login/firebase-token `
  -H "Content-Type: application/json" `
  -d '{\"idToken\":\"test\"}' `
  -v
```

### Bước 4: Kiểm tra Các Nguyên Nhân Thường Gặp

#### 4.1. Firebase Service không hoạt động

**Kiểm tra:**
- File `service-account-key.json` có tồn tại không?
- File có đúng format JSON không?
- Firebase credentials có hợp lệ không?

```powershell
# Kiểm tra file
Test-Path C:\inetpub\wwwroot\quanlyfilesBE\service-account-key.json

# Kiểm tra format
Get-Content C:\inetpub\wwwroot\quanlyfilesBE\service-account-key.json | ConvertFrom-Json
```

#### 4.2. Database Connection Issue

**Kiểm tra connection string:**

```powershell
# Xem appsettings.json
Get-Content C:\inetpub\wwwroot\quanlyfilesBE\appsettings.json

# Test database connection
# (Nếu có endpoint test-db)
Invoke-RestMethod -Uri "http://localhost:8080/api/test-db"
```

#### 4.3. Application Pool không chạy đúng

**Kiểm tra:**

```powershell
Import-Module WebAdministration
Get-WebAppPoolState -Name "quanlyfilesBE"

# Xem chi tiết
Get-ItemProperty "IIS:\AppPools\quanlyfilesBE" | Select-Object *
```

### Bước 5: Xem Chi Tiết Error Response

**Trong browser DevTools:**

1. Mở request bị lỗi (500)
2. Tab **Response** → Copy toàn bộ response body
3. Response thường có format:
   ```json
   {
     "error": "Error during Firebase token login",
     "message": "...",
     "innerException": "...",
     "stackTrace": "..."
   }
   ```

**Phân tích error message:**
- `message`: Lỗi chính
- `innerException`: Lỗi chi tiết hơn
- `stackTrace`: Vị trí code bị lỗi

## Các Lỗi Thường Gặp và Giải Pháp

### Lỗi 1: "Firebase service not initialized"

**Nguyên nhân:** Firebase credentials chưa được cấu hình

**Giải pháp:**
1. Kiểm tra `appsettings.json` có cấu hình Firebase:
   ```json
   {
     "Firebase": {
       "CredentialsPath": "path/to/service-account-key.json",
       "CredentialsJson": "..."
     }
   }
   ```
2. Đảm bảo file `service-account-key.json` tồn tại và có quyền đọc

### Lỗi 2: "Database connection failed"

**Nguyên nhân:** Connection string không đúng hoặc database không accessible

**Giải pháp:**
1. Kiểm tra connection string trong `appsettings.json`
2. Test connection từ server đến database
3. Kiểm tra firewall rules

### Lỗi 3: "Invalid Firebase ID token"

**Nguyên nhân:** Token từ frontend không hợp lệ hoặc đã expire

**Giải pháp:**
1. Kiểm tra frontend có gửi đúng token không
2. Token có thể đã hết hạn - cần refresh token

### Lỗi 4: "User account is inactive"

**Nguyên nhân:** User trong database có `IsActive = false`

**Giải pháp:**
1. Kiểm tra database:
   ```sql
   SELECT UserId, UserName, IsActive FROM Users WHERE FirebaseUID = '...'
   ```
2. Update user nếu cần:
   ```sql
   UPDATE Users SET IsActive = 1 WHERE FirebaseUID = '...'
   ```

## Quick Debug Checklist

- [ ] Backend đã được deploy với CORS config mới
- [ ] Application Pool đang chạy (Started)
- [ ] Database connection hoạt động
- [ ] Firebase credentials đã được cấu hình
- [ ] File `service-account-key.json` tồn tại và có quyền đọc
- [ ] Logs không có lỗi nghiêm trọng
- [ ] Error response có thông tin chi tiết

## Lấy Error Response Chi Tiết

**Từ browser console:**

```javascript
fetch('http://172.20.115.40:8080/api/auth/login/firebase-token', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    idToken: 'your-token-here'
  })
})
.then(res => res.json())
.then(data => {
  console.log('Error details:', data);
  // Xem message, innerException, stackTrace
})
.catch(err => console.error('Network error:', err));
```

**Copy error message và gửi cho dev để debug chi tiết hơn.**

