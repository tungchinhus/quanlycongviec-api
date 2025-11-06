# Hướng dẫn tạo User mới

Có 3 cách để tạo user mới trong hệ thống, tùy theo giao diện và quyền của bạn:

## 1. Tạo User từ Form UI (Không cần Admin) - ✅ **PHÙ HỢP VỚI GIAO DIỆN**

### Endpoint
```
POST /api/users/create-simple
```

### Đặc điểm
- ✅ **Không cần authentication** (`[AllowAnonymous]`)
- ✅ **Format giống với form UI** (userName, fullName, email, password, role)
- ✅ Tự động tạo user trên Firebase Authentication
- ✅ Tự động set custom claims trên Firebase
- ✅ Tự động tạo user trong Local DB
- ✅ Tự động gán role

### Request Body Format (Giống với Form UI)

```json
{
  "userName": "user123",
  "fullName": "user123",
  "email": "user123@gmail.com",
  "password": "Ab!123456",
  "role": "User"
}
```

### Fields Mapping với Form UI

| Form UI Field | API Field | Type | Required | Description |
|--------------|-----------|------|----------|-------------|
| Tên đăng nhập* | `userName` | string | Yes | Username (unique) |
| Họ tên* | `fullName` | string | No | Tên đầy đủ của user |
| Email* | `email` | string | Yes | Email của user |
| Mật khẩu* | `password` | string | Yes | Password (tối thiểu 6 ký tự) |
| Quyền* (dropdown) | `role` | string | No | Tên role (ví dụ: "User", "Admin", "Manager") |

### Ví dụ Request

#### Ví dụ 1: Tạo user với role "User" (từ form UI)
```http
POST http://localhost:5000/api/users/create-simple
Content-Type: application/json

{
  "userName": "user123",
  "fullName": "user123",
  "email": "user123@gmail.com",
  "password": "Ab!123456",
  "role": "User"
}
```

#### Ví dụ 2: Tạo user với role "Admin"
```http
POST http://localhost:5000/api/users/create-simple
Content-Type: application/json

{
  "userName": "admin123",
  "fullName": "Admin User",
  "email": "admin123@gmail.com",
  "password": "Ab!123456",
  "role": "Admin"
}
```

#### Ví dụ 3: Tạo user không có role (sẽ tự động gán "User")
```http
POST http://localhost:5000/api/users/create-simple
Content-Type: application/json

{
  "userName": "newuser",
  "fullName": "New User",
  "email": "newuser@gmail.com",
  "password": "Ab!123456"
}
```

### Response Success (201 Created)

```json
{
  "userId": 5,
  "userName": "user123",
  "fullName": "user123",
  "email": "user123@gmail.com",
  "firebaseUID": "abc123xyz789",
  "isActive": true,
  "createdAt": "2025-11-06T10:00:00Z",
  "roles": ["User"]
}
```

### Response Error (400 Bad Request)

```json
{
  "error": "Username already exists"
}
```

hoặc

```json
{
  "error": "Email already exists"
}
```

---

## 2. Tạo User với Firebase (Không cần Admin)

### Endpoint
```
POST /api/users/firebase
```

### Đặc điểm
- ✅ **Không cần authentication** (`[AllowAnonymous]`)
- ✅ Tự động tạo user trên Firebase Authentication
- ✅ Tự động set custom claims trên Firebase
- ✅ Tự động tạo user trong Local DB
- ✅ Tự động gán roles

### Request Body Format

```json
{
  "name": "user123",
  "email": "user123@gmail.com",
  "password": "Ab!123456",
  "roles": ["User"]
}
```

### Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | Tên hiển thị của user |
| `email` | string | Yes | Email của user (sẽ dùng để tạo trên Firebase) |
| `password` | string | Yes | Password (tối thiểu 6 ký tự) |
| `roles` | string[] | No | Danh sách tên roles (ví dụ: `["User"]`, `["Admin"]`, `["User", "Manager"]`) |

### Ví dụ Request

#### Ví dụ 1: Tạo user với role "User"
```http
POST http://localhost:5000/api/users/firebase
Content-Type: application/json

{
  "name": "user123",
  "email": "user123@gmail.com",
  "password": "Ab!123456",
  "roles": ["User"]
}
```

#### Ví dụ 2: Tạo user với role "Admin"
```http
POST http://localhost:5000/api/users/firebase
Content-Type: application/json

{
  "name": "admin123",
  "email": "admin123@gmail.com",
  "password": "Ab!123456",
  "roles": ["Admin"]
}
```

#### Ví dụ 3: Tạo user không có roles (sẽ được gán role "User" mặc định)
```http
POST http://localhost:5000/api/users/firebase
Content-Type: application/json

{
  "name": "newuser",
  "email": "newuser@gmail.com",
  "password": "Ab!123456"
}
```

### Response Success (201 Created)

```json
{
  "userId": 5,
  "userName": "user123",
  "fullName": "user123",
  "email": "user123@gmail.com",
  "firebaseUID": "abc123xyz789",
  "isActive": true,
  "createdAt": "2025-11-06T10:00:00Z",
  "roles": ["User"]
}
```

### Response Error (400 Bad Request)

```json
{
  "error": "Email already exists in local database"
}
```

---

## 2. Tạo User với Admin Role (Yêu cầu quyền Admin)

### Endpoint
```
POST /api/users
```

### Đặc điểm
- ⚠️ **Yêu cầu authentication** với role `Admin`
- ⚠️ Cần gửi JWT token trong header
- ✅ Tự động tạo user trên Firebase Authentication
- ✅ Tự động set custom claims trên Firebase
- ✅ Tự động tạo user trong Local DB
- ✅ Tự động gán roles

### Request Headers

```
Authorization: Bearer {your_jwt_token}
Content-Type: application/json
```

### Request Body Format

```json
{
  "userName": "user123",
  "email": "user123@gmail.com",
  "fullName": "user123",
  "password": "Ab!123456",
  "roleIds": [3]
}
```

### Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `userName` | string | Yes | Username (unique) |
| `email` | string | Yes | Email của user |
| `fullName` | string | No | Tên đầy đủ |
| `password` | string | Yes | Password |
| `roleIds` | number[] | No | Danh sách Role IDs (ví dụ: `[1]`, `[2, 3]`) |

### Ví dụ Request

```http
POST http://localhost:5000/api/users
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "userName": "user123",
  "email": "user123@gmail.com",
  "fullName": "user123",
  "password": "Ab!123456",
  "roleIds": [3]
}
```

### Response Success (201 Created)

```json
{
  "userId": 5,
  "userName": "user123",
  "fullName": "user123",
  "email": "user123@gmail.com",
  "firebaseUID": "abc123xyz789",
  "isActive": true,
  "createdAt": "2025-11-06T10:00:00Z",
  "roles": ["User"]
}
```

### Response Error (403 Forbidden)

```json
{
  "error": "Forbidden"
}
```

**Nguyên nhân:** Token không có role Admin hoặc không được gửi kèm.

---

## So sánh 3 Endpoints

| Tính năng | `/api/users/create-simple` | `/api/users/firebase` | `/api/users` |
|-----------|---------------------------|----------------------|--------------|
| Authentication | ❌ Không cần | ❌ Không cần | ✅ Cần Admin |
| Format | ✅ Giống Form UI | ❌ Khác format | ✅ Giống Form UI |
| Username | ✅ Phải cung cấp | ❌ Tự động tạo từ email | ✅ Phải cung cấp |
| FullName | ✅ `fullName` | ❌ `name` | ✅ `fullName` |
| Role | ✅ `role: "User"` (string) | ✅ `roles: ["User"]` (array) | ✅ `roleIds: [3]` (array) |
| Firebase UID | ✅ Tự động tạo | ✅ Tự động tạo | ✅ Tự động tạo |
| Custom Claims | ✅ Tự động set | ✅ Tự động set | ✅ Tự động set |
| **Khuyến nghị** | ✅ **Dùng cho Form UI** | ✅ Dùng cho API đơn giản | ⚠️ Cần Admin role |

---

## Mapping Roles

### Role IDs thường dùng:
- `1` = "Admin"
- `2` = "Manager"  
- `3` = "User"

### Role Names:
- `"Admin"`
- `"Manager"`
- `"User"`

**Lưu ý:** Role IDs có thể khác nhau tùy vào database. Để kiểm tra, query bảng `Roles` trong database.

---

## Ví dụ với JavaScript/TypeScript

### Sử dụng Fetch API

```javascript
// Tạo user với endpoint /api/users/firebase (không cần auth)
async function createUser() {
  const response = await fetch('http://localhost:5000/api/users/firebase', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      name: 'user123',
      email: 'user123@gmail.com',
      password: 'Ab!123456',
      roles: ['User']
    })
  });

  if (response.ok) {
    const user = await response.json();
    console.log('User created:', user);
  } else {
    const error = await response.json();
    console.error('Error:', error);
  }
}
```

### Sử dụng với Axios

```javascript
import axios from 'axios';

// Tạo user với endpoint /api/users/create-simple (PHÙ HỢP VỚI FORM UI)
async function createUserFromForm(formData) {
  try {
    const response = await axios.post('http://localhost:5000/api/users/create-simple', {
      userName: formData.userName,      // Từ form: "Tên đăng nhập"
      fullName: formData.fullName,       // Từ form: "Họ tên"
      email: formData.email,            // Từ form: "Email"
      password: formData.password,      // Từ form: "Mật khẩu"
      role: formData.role               // Từ form dropdown: "Quyền"
    });

    console.log('User created:', response.data);
    return response.data;
  } catch (error) {
    console.error('Error:', error.response?.data || error.message);
    throw error;
  }
}

// Sử dụng trong form submit handler
async function handleSubmit(event) {
  event.preventDefault();
  
  const formData = {
    userName: document.getElementById('userName').value,
    fullName: document.getElementById('fullName').value,
    email: document.getElementById('email').value,
    password: document.getElementById('password').value,
    role: document.getElementById('role').value // "User", "Admin", "Manager"
  };

  try {
    const user = await createUserFromForm(formData);
    alert('Tạo user thành công!');
    // Reload danh sách users hoặc redirect
  } catch (error) {
    alert('Lỗi: ' + (error.response?.data || error.message));
  }
}

// Tạo user với endpoint /api/users/firebase (format khác)
async function createUserWithFirebase() {
  try {
    const response = await axios.post('http://localhost:5000/api/users/firebase', {
      name: 'user123',
      email: 'user123@gmail.com',
      password: 'Ab!123456',
      roles: ['User']
    });

    console.log('User created:', response.data);
  } catch (error) {
    console.error('Error:', error.response?.data || error.message);
  }
}

// Tạo user với endpoint /api/users (cần Admin token)
async function createUserWithAdmin() {
  const token = localStorage.getItem('token'); // Lấy token từ storage

  try {
    const response = await axios.post(
      'http://localhost:5000/api/users',
      {
        userName: 'user123',
        email: 'user123@gmail.com',
        fullName: 'user123',
        password: 'Ab!123456',
        roleIds: [3]
      },
      {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      }
    );

    console.log('User created:', response.data);
  } catch (error) {
    if (error.response?.status === 403) {
      console.error('Access denied. Need Admin role.');
    } else {
      console.error('Error:', error.response?.data || error.message);
    }
  }
}
```

---

## Kiểm tra User đã tạo

### Kiểm tra trong Local DB
```http
GET http://localhost:5000/api/users/by-firebase-uid/{firebaseUID}
```

### Kiểm tra Custom Claims trên Firebase
```http
GET http://localhost:5000/api/users/check-custom-claims/{firebaseUID}
```

---

## Troubleshooting

### Lỗi 403 Forbidden khi dùng `/api/users`
**Giải pháp:** 
- Dùng endpoint `/api/users/firebase` thay thế (không cần auth)
- Hoặc đăng nhập với tài khoản Admin và gửi token trong header

### Lỗi "Email already exists"
**Giải pháp:** 
- Email đã tồn tại trong database hoặc Firebase
- Thử email khác hoặc kiểm tra user đã tồn tại

### Lỗi "Username already exists" (chỉ với `/api/users`)
**Giải pháp:** 
- Username đã được sử dụng
- Thử username khác

### Roles không được gán
**Giải pháp:**
- Kiểm tra role name/ID có tồn tại trong database không
- Với `/api/users/firebase`, nếu không có roles, sẽ tự động gán role "User" mặc định

---

## Lưu ý quan trọng

1. **Password:** Tối thiểu 6 ký tự (theo yêu cầu của Firebase)
2. **Email:** Phải là email hợp lệ và chưa được sử dụng
3. **Roles:** 
   - Với `/api/users/firebase`: dùng role names như `["Admin"]`
   - Với `/api/users`: dùng role IDs như `[1]`
4. **Firebase:** User sẽ được tạo tự động trên Firebase Authentication
5. **Custom Claims:** Roles sẽ được tự động set vào Firebase custom claims

