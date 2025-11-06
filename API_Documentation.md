# API Documentation - User và Role Management

## Base URL
```
http://localhost:5000/api
hoặc
https://localhost:5001/api
```

---

## 🔐 Authentication APIs (`/api/auth`)

### 1. Đăng ký User (Local)
**POST** `/api/auth/register`
- **Authorization**: Không cần
- **Request Body**:
```json
{
  "userName": "john_doe",
  "password": "password123",
  "fullName": "John Doe",
  "email": "john@example.com"
}
```
- **Response**: `200 OK`
```json
{
  "userId": 1,
  "userName": "john_doe"
}
```

### 2. Đăng nhập (Local)
**POST** `/api/auth/login`
- **Authorization**: Không cần
- **Request Body**:
```json
{
  "userName": "john_doe",  // Có thể là username hoặc email
  "password": "password123"
}
```
- **Lưu ý**: Trường `userName` có thể nhận:
  - Username (ví dụ: `"john_doe"`, `"admin"`)
  - Email (ví dụ: `"john@example.com"`, `"admin@example.com"`)
- **Response**: `200 OK`
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "userId": 1,
    "userName": "john_doe",
    "fullName": "John Doe",
    "email": "john@example.com",
    "roles": ["User"]
  }
}
```

### 3. Đăng nhập với Firebase
**POST** `/api/auth/login/firebase`
- **Authorization**: Không cần
- **Request Body**:
```json
{
  "firebaseUID": "abc123xyz789",
  "email": "user@example.com",
  "fullName": "User Name"
}
```
- **Response**: `200 OK`
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "userId": 1,
    "userName": "user",
    "fullName": "User Name",
    "email": "user@example.com",
    "firebaseUID": "abc123xyz789",
    "roles": ["User"]
  }
}
```

### 4. Lấy thông tin User hiện tại
**GET** `/api/auth/me`
- **Authorization**: Required (Bearer Token)
- **Response**: `200 OK`
```json
{
  "name": "john_doe",
  "roles": ["Admin", "User"],
  "permissions": ["files.view", "files.manage", "users.manage"]
}
```

---

## 👥 User Management APIs (`/api/users`)

### 1. Lấy danh sách Users (Phân trang)
**GET** `/api/users?page=1&pageSize=10&search=john`
- **Authorization**: Không cần (hiện tại)
- **Query Parameters**:
  - `page` (int, default: 1): Số trang
  - `pageSize` (int, default: 10): Số items mỗi trang
  - `search` (string, optional): Tìm kiếm theo UserName, FullName, Email
- **Response**: `200 OK`
```json
{
  "data": [
    {
      "userId": 1,
      "userName": "john_doe",
      "fullName": "John Doe",
      "email": "john@example.com",
      "firebaseUID": "abc123xyz789",
      "isActive": true,
      "createdAt": "2024-01-01T00:00:00Z",
      "roles": ["Admin", "User"]
    }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

### 2. Lấy thông tin User theo ID
**GET** `/api/users/{id}`
- **Authorization**: Required
- **Response**: `200 OK`
```json
{
  "userId": 1,
  "userName": "john_doe",
  "fullName": "John Doe",
  "email": "john@example.com",
  "firebaseUID": "abc123xyz789",
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "roles": ["Admin", "User"]
}
```

### 3. Tạo User mới (Local DB)
**POST** `/api/users`
- **Authorization**: Required (Role: Admin)
- **Request Body**:
```json
{
  "userName": "new_user",
  "fullName": "New User",
  "email": "newuser@example.com",
  "firebaseUID": "optional_firebase_uid",
  "password": "password123",
  "roleIds": [1, 2]
}
```
- **Response**: `201 Created`
```json
{
  "userId": 2,
  "userName": "new_user",
  "fullName": "New User",
  "email": "newuser@example.com",
  "firebaseUID": "optional_firebase_uid",
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "roles": ["Admin", "User"]
}
```

### 4. Tạo User với Firebase Authentication
**POST** `/api/users/firebase`
- **Authorization**: Không cần
- **Request Body**:
```json
{
  "name": "Nguyễn Văn A",
  "email": "user@example.com",
  "password": "password123",
  "roles": ["User"]
}
```
- **Response**: `201 Created`
```json
{
  "userId": 3,
  "userName": "user",
  "fullName": "Nguyễn Văn A",
  "email": "user@example.com",
  "firebaseUID": "firebase_uid_here",
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "roles": ["User"]
}
```
- **Note**: 
  - Tạo user trên Firebase Authentication
  - Set custom claims với roles
  - Tạo user trong local DB

### 5. Cập nhật User
**PUT** `/api/users/{id}`
- **Authorization**: Required (Role: Admin)
- **Request Body**:
```json
{
  "userName": "updated_user",
  "fullName": "Updated User",
  "email": "updated@example.com",
  "isActive": true,
  "roleIds": [2]
}
```
- **Response**: `200 OK`
```json
{
  "userId": 1,
  "userName": "updated_user",
  "fullName": "Updated User",
  "email": "updated@example.com",
  "firebaseUID": "abc123xyz789",
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "roles": ["User"]
}
```

### 6. Xóa User
**DELETE** `/api/users/{id}`
- **Authorization**: Required (Role: Admin)
- **Response**: `204 No Content`

### 7. Kích hoạt User
**PATCH** `/api/users/{id}/activate`
- **Authorization**: Required (Role: Admin)
- **Response**: `200 OK`
```json
{
  "message": "User activated successfully"
}
```

### 8. Vô hiệu hóa User
**PATCH** `/api/users/{id}/deactivate`
- **Authorization**: Required (Role: Admin)
- **Response**: `200 OK`
```json
{
  "message": "User deactivated successfully"
}
```

### 9. Cập nhật Roles của User (Firebase Custom Claims)
**PUT** `/api/users/{userId}/roles`
- **Authorization**: Required (Role: Admin)
- **Request Body**:
```json
{
  "roles": ["Administrator", "Manager"]
}
```
- **Response**: `200 OK`
```json
{
  "userId": 1,
  "userName": "john_doe",
  "fullName": "John Doe",
  "email": "john@example.com",
  "firebaseUID": "abc123xyz789",
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "roles": ["Administrator", "Manager"]
}
```
- **Note**: 
  - Cập nhật roles trong local DB
  - Set custom claims trên Firebase
  - User cần refresh token để nhận claims mới

### 10. Set Custom Claims trực tiếp
**POST** `/api/users/{firebaseUid}/set-custom-claims`
- **Authorization**: Không cần
- **Request Body**:
```json
{
  "roles": ["Administrator", "Manager"],
  "name": "Nguyễn Văn A"
}
```
- **Response**: `200 OK`
```json
{
  "success": true,
  "message": "Custom claims set successfully"
}
```

### 11. Lấy User theo Firebase UID
**GET** `/api/users/by-firebase-uid/{firebaseUid}`
- **Authorization**: Không cần
- **Response**: `200 OK`
```json
{
  "userId": 1,
  "userName": "john_doe",
  "fullName": "John Doe",
  "email": "john@example.com",
  "firebaseUID": "abc123xyz789",
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "roles": ["User"]
}
```

### 12. Cập nhật hoặc tạo User theo Firebase UID (Đồng bộ)
**PUT** `/api/users/by-firebase-uid/{firebaseUid}`
- **Authorization**: Không cần
- **Request Body**:
```json
{
  "name": "Nguyễn Văn A",
  "email": "user@example.com",
  "roles": ["User"]
}
```
- **Response**: `200 OK`
```json
{
  "userId": 1,
  "userName": "user",
  "fullName": "Nguyễn Văn A",
  "email": "user@example.com",
  "firebaseUID": "abc123xyz789",
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "roles": ["User"]
}
```
- **Note**: Tạo mới nếu chưa có, cập nhật nếu đã tồn tại

---

## 📋 DTOs (Data Transfer Objects)

### UserDto
```json
{
  "userId": 1,
  "userName": "string",
  "fullName": "string | null",
  "email": "string | null",
  "firebaseUID": "string | null",
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "roles": ["string"]
}
```

### UserCreateDto
```json
{
  "userName": "string",
  "fullName": "string | null",
  "email": "string | null",
  "firebaseUID": "string | null",
  "password": "string | null",
  "roleIds": [1, 2]
}
```

### UserUpdateDto
```json
{
  "userName": "string | null",
  "fullName": "string | null",
  "email": "string | null",
  "isActive": true | null,
  "roleIds": [1, 2] | null
}
```

### CreateUserWithFirebaseDto
```json
{
  "name": "string",
  "email": "string",
  "password": "string",
  "roles": ["string"]
}
```

### UpdateUserRolesDto
```json
{
  "roles": ["string"]
}
```

### SetCustomClaimsDto
```json
{
  "roles": ["string"],
  "name": "string | null"
}
```

### SyncUserFromFirebaseDto
```json
{
  "name": "string",
  "email": "string",
  "roles": ["string"]
}
```

---

## 🔑 Authentication & Authorization

### JWT Token Structure
Token chứa các claims:
- `sub`: UserId
- `name`: UserName
- `role`: Danh sách roles (có thể có nhiều)
- `permission`: Danh sách permissions (có thể có nhiều)

### Authorization Headers
```
Authorization: Bearer {token}
```

### Roles mặc định
- `Admin`: Quản trị viên hệ thống
- `User`: Người dùng thông thường

### Permissions mặc định
- `files.view`: Xem files
- `files.manage`: Quản lý files
- `users.manage`: Quản lý users

---

## ⚠️ Lưu ý quan trọng

1. **Firebase Custom Claims**:
   - Khi set custom claims, user cần refresh ID token để nhận claims mới
   - Frontend gọi `getIdToken(true)` để force refresh

2. **Đồng bộ dữ liệu**:
   - Firebase lưu roles trong Custom Claims (string array)
   - Local DB lưu chi tiết: Roles, Permissions, UserRoles, RolePermissions
   - Khi cập nhật roles, cần cập nhật cả 2 nơi

3. **Token Expiry**:
   - JWT token mặc định hết hạn sau 120 phút
   - Có thể cấu hình trong `appsettings.json`

4. **CORS**:
   - Chỉ cho phép từ `http://localhost:4200` (Angular frontend)
   - Có thể cấu hình trong `Program.cs`

---

## 📝 Error Responses

### 400 Bad Request
```json
{
  "error": "Username already exists"
}
```

### 401 Unauthorized
```json
{
  "error": "Invalid credentials"
}
```

### 404 Not Found
```json
{
  "error": "User not found"
}
```

### 500 Internal Server Error
```json
{
  "error": "Error creating Firebase user: {error message}"
}
```

---

## 🧪 Testing với Swagger

Truy cập: `http://localhost:5000/swagger` để test các API endpoints trực tiếp.

