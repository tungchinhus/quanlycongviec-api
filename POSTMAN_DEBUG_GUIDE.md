# Hướng Dẫn Debug 401 Unauthorized trong Postman

## 🔍 Kiểm Tra Từng Bước

### Bước 1: Kiểm tra Server đang chạy

Đảm bảo server đang chạy tại `http://localhost:5000`

---

### Bước 2: Đăng nhập để lấy Token

**POST** `http://localhost:5000/api/auth/login`

**Headers:**
```
Content-Type: application/json
```

**Body:**
```json
{
  "userName": "admin",
  "password": "your_admin_password"
}
```

**Lưu ý quan trọng:**
- User phải có role **"Admin"** trong database
- Copy token từ response (trong field `token`)

**Response mẫu:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZSI6ImFkbWluIiwicm9sZSI6IkFkbWluIiwiaXNzIjoicXVhbmx5ZmlsZXMiLCJhdWQiOiJxdWFubHlmaWxlcy1jbGllbnQiLCJleHAiOjE3MDAwMDAwMDB9.xxx",
  "user": {
    "userId": 1,
    "userName": "admin",
    "fullName": "Admin User",
    "email": "admin@example.com"
  }
}
```

---

### Bước 3: Test Token với Endpoint Test

**GET** `http://localhost:5000/api/users/test-auth`

**Headers:**
```
Authorization: Bearer {YOUR_TOKEN}
```

**Response thành công:**
```json
{
  "message": "Authentication successful",
  "userId": "1",
  "userName": "admin",
  "roles": ["Admin"],
  "isAdmin": true,
  "allClaims": [
    { "Type": "sub", "Value": "1" },
    { "Type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "Value": "admin" },
    { "Type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", "Value": "Admin" }
  ]
}
```

**Nếu lỗi 401:**
- Token không hợp lệ hoặc đã hết hạn
- Kiểm tra lại bước 2

**Nếu `isAdmin: false`:**
- User không có role Admin
- Cần gán role Admin cho user trong database

---

### Bước 4: Tạo User Mới

**POST** `http://localhost:5000/api/users`

**Headers:**
```
Content-Type: application/json
Authorization: Bearer {YOUR_TOKEN}
```

**Body:**
```json
{
  "userName": "fdsfdf",
  "email": "tungchinhusdd@gmail.com",
  "password": "Ab!123456",
  "fullName": "fsdfsfsdfds",
  "roleIds": [3]
}
```

---

## 🔧 Cách Thêm Authorization Header trong Postman

### Cách 1: Tab Authorization (Khuyên dùng)

1. Click vào tab **Authorization**
2. Chọn **Type:** `Bearer Token`
3. Paste token vào ô **Token**
4. Postman tự động thêm vào Headers

### Cách 2: Tab Headers (Thủ công)

1. Click vào tab **Headers**
2. Thêm header mới:
   - **Key:** `Authorization`
   - **Value:** `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`
   - (Thay token thực tế)

**⚠️ Lưu ý:**
- Phải có từ "Bearer " (với dấu space) trước token
- Không được có dấu ngoặc kép quanh token

---

## 🐛 Common Issues và Giải Pháp

### Issue 1: 401 Unauthorized - "The request is unauthenticated"

**Nguyên nhân:**
- Chưa thêm Authorization header
- Token không hợp lệ
- Token đã hết hạn

**Giải pháp:**
1. Kiểm tra Authorization header đã được thêm chưa
2. Đăng nhập lại để lấy token mới
3. Kiểm tra token format: `Bearer {token}` (có space)

---

### Issue 2: 403 Forbidden - "User does not have required role"

**Nguyên nhân:**
- User không có role "Admin"

**Giải pháp:**
1. Kiểm tra user có role Admin trong database:
```sql
SELECT u.UserName, r.RoleName 
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN Roles r ON ur.RoleId = r.RoleId
WHERE u.UserName = 'admin'
```

2. Nếu không có, gán role Admin:
```sql
-- Lấy UserId và RoleId
DECLARE @UserId INT = (SELECT UserId FROM Users WHERE UserName = 'admin')
DECLARE @RoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = 'Admin')

-- Gán role
INSERT INTO UserRoles (UserId, RoleId, AssignedAt)
VALUES (@UserId, @RoleId, GETUTCDATE())
```

3. Đăng nhập lại để lấy token mới với role Admin

---

### Issue 3: Token valid nhưng vẫn 401

**Nguyên nhân:**
- JWT configuration không đúng
- Issuer/Audience không khớp

**Giải pháp:**
1. Kiểm tra `appsettings.json`:
```json
{
  "Jwt": {
    "Issuer": "quanlyfiles",
    "Audience": "quanlyfiles-client",
    "Key": "CHANGE_THIS_DEVELOPMENT_SECRET_KEY_32_CHARS_MIN"
  }
}
```

2. Kiểm tra console logs khi gọi API để xem error message

---

### Issue 4: Token hết hạn

**Nguyên nhân:**
- Token mặc định hết hạn sau 120 phút

**Giải pháp:**
- Đăng nhập lại để lấy token mới

---

## 📋 Checklist Debug

- [ ] Server đang chạy tại `http://localhost:5000`
- [ ] Đã đăng nhập và copy token
- [ ] Token chưa hết hạn (< 120 phút)
- [ ] Đã thêm Authorization header với format: `Bearer {token}`
- [ ] User có role "Admin" trong database
- [ ] Đã test với endpoint `/api/users/test-auth` thành công
- [ ] Body request đúng format JSON
- [ ] Content-Type header là `application/json`

---

## 🧪 Test Script cho Postman

Thêm vào tab **Tests** của request login để tự động lưu token:

```javascript
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    pm.environment.set("jwt_token", jsonData.token);
    console.log("Token saved:", jsonData.token);
}
```

Sau đó trong request tạo user, sử dụng:
- **Type:** `Bearer Token`
- **Token:** `{{jwt_token}}`

---

## 📞 Kiểm Tra Logs

Khi gọi API, kiểm tra console output của server để xem:
- "Token validated. Claims: ..." - Token hợp lệ
- "Authentication failed: ..." - Token không hợp lệ
- "Challenge: ..." - Có vấn đề với authentication

Những logs này giúp debug vấn đề nhanh chóng.

