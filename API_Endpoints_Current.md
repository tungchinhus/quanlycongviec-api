# API Endpoints Hiện Tại - Hệ Thống Quản Lý Files

**Base URL**: `http://localhost:5000/api` hoặc `https://localhost:5001/api`

---

## 🔐 Authentication APIs (`/api/auth`)

### 1. Đăng ký User (Local DB)
**POST** `/api/auth/register`
- **Authorization**: Không cần (`[AllowAnonymous]`)
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

### 2. Đăng nhập (Local DB)
**POST** `/api/auth/login`
- **Authorization**: Không cần (`[AllowAnonymous]`)
- **Request Body**:
```json
{
  "userName": "john_doe",
  "password": "password123"
}
```
- **Response**: `200 OK`
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "userId": 1,
    "userName": "john_doe",
    "fullName": "John Doe",
    "email": "john@example.com"
  }
}
```

### 3. Đăng nhập với Firebase
**POST** `/api/auth/login/firebase`
- **Authorization**: Không cần (`[AllowAnonymous]`)
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
- **Note**: Tự động tạo user trong DB nếu chưa tồn tại, gán role "User" mặc định

### 4. Lấy thông tin User hiện tại
**GET** `/api/auth/me`
- **Authorization**: Cần JWT Token (`[Authorize]`)
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
- **Authorization**: Không cần (`[AllowAnonymous]`) - hiện tại cho phép test
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

### 2. Lấy User theo ID
**GET** `/api/users/{id}`
- **Authorization**: Cần JWT Token (`[Authorize]`)
- **Response**: `200 OK` hoặc `404 Not Found`
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
- **Authorization**: Cần JWT Token với role "Admin" (`[Authorize(Roles = "Admin")]`)
- **Request Body**:
```json
{
  "userName": "new_user",
  "fullName": "New User",
  "email": "new@example.com",
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
  "email": "new@example.com",
  "firebaseUID": "optional_firebase_uid",
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "roles": ["Admin", "User"]
}
```

### 4. Cập nhật User
**PUT** `/api/users/{id}`
- **Authorization**: Cần JWT Token (`[Authorize]`)
- **Quyền hạn**:
  - **Admin/Manager**: Có thể update bất kỳ user nào, bao gồm `roleIds` và `isActive`
  - **User thường**: Chỉ có thể update thông tin của chính mình (không được thay đổi `roleIds` và `isActive`)
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
- **Response**: `200 OK` hoặc `403 Forbidden` (nếu không có quyền)
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
- **Note**: Nếu user có `firebaseUID` và update roles, tự động đồng bộ lên Firebase Custom Claims

### 5. Xóa User
**DELETE** `/api/users/{id}`
- **Authorization**: Cần JWT Token với role "Admin" (`[Authorize(Roles = "Admin")]`)
- **Response**: `204 No Content` hoặc `404 Not Found`

### 6. Kích hoạt User
**PATCH** `/api/users/{id}/activate`
- **Authorization**: Cần JWT Token với role "Admin" (`[Authorize(Roles = "Admin")]`)
- **Response**: `200 OK`
```json
{
  "message": "User activated successfully"
}
```

### 7. Vô hiệu hóa User
**PATCH** `/api/users/{id}/deactivate`
- **Authorization**: Cần JWT Token với role "Admin" (`[Authorize(Roles = "Admin")]`)
- **Response**: `200 OK`
```json
{
  "message": "User deactivated successfully"
}
```

### 8. Tạo User với Firebase Authentication
**POST** `/api/users/firebase`
- **Authorization**: Không cần (`[AllowAnonymous]`)
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
- **Backend Logic**:
  1. Tạo user trên Firebase Authentication
  2. Set custom claims với roles và name
  3. Tạo user trong local DB
  4. Gán roles trong local DB

### 9. Cập nhật Roles của User (Firebase Custom Claims)
**PUT** `/api/users/{userId}/roles`
- **Authorization**: Cần JWT Token với role "Admin" (`[Authorize(Roles = "Admin")]`)
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
- **Backend Logic**:
  1. Cập nhật roles trong local DB
  2. Set custom claims trên Firebase
  3. User cần refresh token để nhận claims mới

### 10. Set Custom Claims trực tiếp
**POST** `/api/users/{firebaseUid}/set-custom-claims`
- **Authorization**: Không cần (`[AllowAnonymous]`)
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
- **Authorization**: Không cần (`[AllowAnonymous]`)
- **Response**: `200 OK` hoặc `404 Not Found`
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
- **Authorization**: Không cần (`[AllowAnonymous]`)
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

### 13. Đồng bộ User từ Firebase về Local DB
**POST** `/api/users/sync-from-firebase/{firebaseUid}`
- **Authorization**: Không cần (`[AllowAnonymous]`)
- **Request Body** (optional):
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
  "message": "User synced successfully from Firebase",
  "user": {
    "userId": 5,
    "userName": "firebase_synced",
    "fullName": "Nguyễn Văn A",
    "email": "user@example.com",
    "firebaseUID": "firebase_uid_string",
    "isActive": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "roles": ["User"]
  }
}
```
- **Backend Logic**:
  1. Kiểm tra user đã tồn tại trong DB chưa
  2. Lấy thông tin từ Firebase Auth
  3. Lấy roles từ Firebase Custom Claims (nếu có)
  4. Tạo hoặc cập nhật user trong local DB

---

## 🔐 Roles Management APIs (`/api/roles`)

**Lưu ý**: Tất cả endpoints trong phần này yêu cầu role "Admin" (`[Authorize(Roles = "Admin")]`)

### 1. Lấy danh sách Roles
**GET** `/api/roles`
- **Authorization**: Cần JWT Token với role "Admin"
- **Response**: `200 OK`
```json
[
  {
    "roleId": 1,
    "roleName": "Admin",
    "description": "Administrator role",
    "permissions": ["files.view", "files.manage", "users.manage"]
  },
  {
    "roleId": 2,
    "roleName": "User",
    "description": "Regular user role",
    "permissions": ["files.view"]
  }
]
```

### 2. Lấy Role theo ID
**GET** `/api/roles/{id}`
- **Authorization**: Cần JWT Token với role "Admin"
- **Response**: `200 OK` hoặc `404 Not Found`
```json
{
  "roleId": 1,
  "roleName": "Admin",
  "description": "Administrator role",
  "permissions": ["files.view", "files.manage", "users.manage"]
}
```

### 3. Tạo Role mới
**POST** `/api/roles`
- **Authorization**: Cần JWT Token với role "Admin"
- **Request Body**:
```json
{
  "roleName": "Manager",
  "description": "Manager role with limited permissions"
}
```
- **Response**: `201 Created`
```json
{
  "roleId": 3,
  "roleName": "Manager",
  "description": "Manager role with limited permissions",
  "permissions": []
}
```

### 4. Cập nhật Role
**PUT** `/api/roles/{id}`
- **Authorization**: Cần JWT Token với role "Admin"
- **Request Body**:
```json
{
  "roleName": "Updated Manager",
  "description": "Updated description"
}
```
- **Response**: `200 OK`
```json
{
  "roleId": 3,
  "roleName": "Updated Manager",
  "description": "Updated description",
  "permissions": ["files.view"]
}
```

### 5. Xóa Role
**DELETE** `/api/roles/{id}`
- **Authorization**: Cần JWT Token với role "Admin"
- **Response**: `204 No Content` hoặc `400 Bad Request` (nếu role đang được sử dụng bởi users)
- **Error Response** (400):
```json
{
  "error": "Cannot delete role that is assigned to users"
}
```

### 6. Lấy Permissions của Role
**GET** `/api/roles/{roleId}/permissions`
- **Authorization**: Cần JWT Token với role "Admin"
- **Response**: `200 OK`
```json
[
  {
    "permissionId": 1,
    "permissionName": "files.view",
    "description": "View files permission"
  },
  {
    "permissionId": 2,
    "permissionName": "files.manage",
    "description": "Manage files permission"
  }
]
```

### 7. Gán Permissions cho Role
**PUT** `/api/roles/{roleId}/permissions`
- **Authorization**: Cần JWT Token với role "Admin"
- **Request Body**:
```json
{
  "permissionIds": [1, 2, 3]
}
```
- **Response**: `200 OK`
```json
{
  "roleId": 1,
  "roleName": "Admin",
  "description": "Administrator role",
  "permissions": ["files.view", "files.manage", "users.manage"]
}
```

---

## 🔐 Permissions Management APIs (`/api/permissions`)

**Lưu ý**: Tất cả endpoints trong phần này yêu cầu role "Admin" (`[Authorize(Roles = "Admin")]`)

### 1. Lấy danh sách Permissions
**GET** `/api/permissions`
- **Authorization**: Cần JWT Token với role "Admin"
- **Response**: `200 OK`
```json
[
  {
    "permissionId": 1,
    "permissionName": "files.view",
    "description": "View files permission"
  },
  {
    "permissionId": 2,
    "permissionName": "files.manage",
    "description": "Manage files permission"
  },
  {
    "permissionId": 3,
    "permissionName": "users.manage",
    "description": "Manage users permission"
  }
]
```

### 2. Lấy Permission theo ID
**GET** `/api/permissions/{id}`
- **Authorization**: Cần JWT Token với role "Admin"
- **Response**: `200 OK` hoặc `404 Not Found`
```json
{
  "permissionId": 1,
  "permissionName": "files.view",
  "description": "View files permission"
}
```

### 3. Tạo Permission mới
**POST** `/api/permissions`
- **Authorization**: Cần JWT Token với role "Admin"
- **Request Body**:
```json
{
  "permissionName": "roles.manage",
  "description": "Manage roles permission"
}
```
- **Response**: `201 Created`
```json
{
  "permissionId": 4,
  "permissionName": "roles.manage",
  "description": "Manage roles permission"
}
```

### 4. Cập nhật Permission
**PUT** `/api/permissions/{id}`
- **Authorization**: Cần JWT Token với role "Admin"
- **Request Body**:
```json
{
  "permissionName": "updated.permission",
  "description": "Updated description"
}
```
- **Response**: `200 OK`
```json
{
  "permissionId": 4,
  "permissionName": "updated.permission",
  "description": "Updated description"
}
```

### 5. Xóa Permission
**DELETE** `/api/permissions/{id}`
- **Authorization**: Cần JWT Token với role "Admin"
- **Response**: `204 No Content` hoặc `400 Bad Request` (nếu permission đang được sử dụng bởi roles)
- **Error Response** (400):
```json
{
  "error": "Cannot delete permission that is assigned to roles"
}
```

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

### RoleDto
```json
{
  "roleId": 1,
  "roleName": "string",
  "description": "string | null",
  "permissions": ["string"]
}
```

### CreateRoleDto
```json
{
  "roleName": "string",
  "description": "string | null"
}
```

### UpdateRoleDto
```json
{
  "roleName": "string | null",
  "description": "string | null"
}
```

### AssignPermissionsDto
```json
{
  "permissionIds": [1, 2, 3]
}
```

### PermissionDto
```json
{
  "permissionId": 1,
  "permissionName": "string",
  "description": "string | null"
}
```

### CreatePermissionDto
```json
{
  "permissionName": "string",
  "description": "string | null"
}
```

### UpdatePermissionDto
```json
{
  "permissionName": "string | null",
  "description": "string | null"
}
```

---

## 🔑 Authentication & Authorization

### JWT Token Structure
Token chứa các claims:
- `sub`: UserId (theo `JwtRegisteredClaimNames.Sub`)
- `name`: UserName (theo `ClaimTypes.Name`)
- `role`: Danh sách roles (có thể có nhiều)
- `permission`: Danh sách permissions (có thể có nhiều)

### Authorization Headers
```
Authorization: Bearer {token}
```

### Roles mặc định
- `Admin`: Quản trị viên hệ thống
- `User`: Người dùng thông thường
- `Manager`: Quản lý (có thể có)
- `Administrator`: Tương tự Admin

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

5. **Authorization Logic**:
   - `PUT /api/users/{id}`: 
     - Admin/Manager: update bất kỳ user, có thể thay đổi roles
     - User thường: chỉ update chính mình, không thể thay đổi roles
   - Các endpoint khác có yêu cầu cụ thể được ghi rõ trong documentation

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

### 403 Forbid
```json
{
  "error": "You can only update your own profile"
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

---

## 📚 Files API (Tham khảo)

Các API Files và Folders có trong `FilesController.cs` và `FoldersController.cs`, nhưng không được liệt kê chi tiết trong tài liệu này.

