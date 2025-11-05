# Hướng Dẫn Tạo User Mới - POST /api/users

## ⚠️ Lỗi 401 Unauthorized

Endpoint này yêu cầu **JWT Token** với role **Admin**. Bạn cần đăng nhập trước để lấy token.

---

## 📋 Các Bước Thực Hiện

### Bước 1: Đăng nhập để lấy JWT Token

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

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "userId": 1,
    "userName": "admin",
    "fullName": "Admin User",
    "email": "admin@example.com"
  }
}
```

**Lưu ý:** User phải có role "Admin" để có quyền tạo user mới.

---

### Bước 2: Tạo User Mới

**POST** `http://localhost:5000/api/users`

**Headers:**
```
Content-Type: application/json
Authorization: Bearer {YOUR_TOKEN_HERE}
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

## 🔧 Cách Thêm Authorization trong Postman

### Cách 1: Thêm Header thủ công

1. Trong tab **Headers**, thêm:
   - **Key:** `Authorization`
   - **Value:** `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`
   - (Thay `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...` bằng token thực tế)

### Cách 2: Sử dụng tab Authorization

1. Click vào tab **Authorization**
2. Chọn **Type:** `Bearer Token`
3. Paste token vào ô **Token**
4. Postman sẽ tự động thêm vào Headers

### Cách 3: Environment Variables (Khuyên dùng)

1. Tạo một request đăng nhập
2. Trong **Tests** tab, thêm script:
```javascript
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    pm.environment.set("jwt_token", jsonData.token);
}
```

3. Trong request tạo user, sử dụng:
   - **Type:** `Bearer Token`
   - **Token:** `{{jwt_token}}`

---

## 📝 Ví Dụ Request Body

### Request Body Đầy Đủ:
```json
{
  "userName": "john_doe",
  "email": "john.doe@example.com",
  "password": "SecurePassword123!",
  "fullName": "John Doe",
  "roleIds": [1, 2]
}
```

### Request Body Tối Thiểu:
```json
{
  "userName": "jane_smith",
  "email": "jane.smith@example.com",
  "password": "SecurePassword123!",
  "fullName": "Jane Smith"
}
```

---

## ✅ Response Success (201 Created)

```json
{
  "userId": 5,
  "userName": "fdsfdf",
  "fullName": "fsdfsfsdfds",
  "email": "tungchinhusdd@gmail.com",
  "firebaseUID": "abc123xyz789",
  "isActive": true,
  "createdAt": "2024-11-04T10:30:00Z",
  "roles": ["User"]
}
```

---

## ❌ Response Error

### 401 Unauthorized:
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "The request is unauthenticated. Pass the correct auth credentials."
}
```

**Giải pháp:** Đảm bảo đã thêm Authorization header với JWT token hợp lệ.

### 403 Forbidden:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "User does not have required role: Admin"
}
```

**Giải pháp:** User đăng nhập phải có role "Admin".

---

## 🔍 Kiểm Tra Token

Nếu không chắc token có hợp lệ, test bằng cách gọi:

**GET** `http://localhost:5000/api/auth/me`

**Headers:**
```
Authorization: Bearer {YOUR_TOKEN}
```

Nếu trả về 200 OK với thông tin user và roles, token hợp lệ.

---

## 💡 Tips

1. **Copy token từ response login** và paste vào Authorization header
2. **Kiểm tra token có hết hạn không** (thường 120 phút)
3. **Đảm bảo user có role Admin** trong database
4. **Sử dụng Environment Variables** trong Postman để quản lý token dễ dàng hơn

