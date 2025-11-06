# API Login Documentation

## Base URL
```
http://localhost:5000/api/auth
```
hoặc
```
https://localhost:5001/api/auth
```

---

## 📋 Danh Sách API Login

### 1. Đăng nhập với Username/Password (Local DB)
### 2. Đăng nhập với Firebase ID Token (Email/Password)
### 3. Đăng nhập với Firebase UID
### 4. Lấy thông tin User hiện tại (Me)

---

## 🔐 1. Đăng Nhập với Username/Email và Password

### Endpoint
```
POST /api/auth/login
```

### Authorization
**Không cần** - Endpoint này public (`[AllowAnonymous]`)

### Headers
```
Content-Type: application/json
```

### Request Body

**Schema:**
```json
{
  "userName": "string",
  "password": "string"
}
```

**Lưu ý:** Trường `userName` có thể là:
- **Username** (ví dụ: `"admin"`, `"user123"`)
- **Email** (ví dụ: `"admin@example.com"`, `"user123@gmail.com"`)

**Ví dụ 1: Đăng nhập bằng Username**
```json
{
  "userName": "admin",
  "password": "password123"
}
```

**Ví dụ 2: Đăng nhập bằng Email**
```json
{
  "userName": "admin@example.com",
  "password": "password123"
}
```

### Response Success (200 OK)

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZSI6ImFkbWluIiwicm9sZSI6IkFkbWluIiwiaXNzIjoicXVhbmx5ZmlsZXMiLCJhdWQiOiJxdWFubHlmaWxlcy1jbGllbnQiLCJleHAiOjE3MDAwMDAwMDB9.xxx",
  "user": {
    "userId": 1,
    "userName": "admin",
    "fullName": "Admin User",
    "email": "admin@example.com",
    "roles": ["Admin"]
  }
}
```

**Lưu ý:** Response bây giờ bao gồm `roles` trong user object.

### Response Error

#### 401 Unauthorized - Sai username/email hoặc password
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid username/email or password"
}
```

#### 401 Unauthorized - Tài khoản bị vô hiệu hóa
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "User account is inactive"
}
```

### cURL Example

**Đăng nhập bằng Username:**
```bash
curl -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "admin",
    "password": "password123"
  }'
```

**Đăng nhập bằng Email:**
```bash
curl -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "admin@example.com",
    "password": "password123"
  }'
```

### JavaScript Example

**Đăng nhập bằng Username hoặc Email:**
```javascript
// Có thể dùng username hoặc email trong trường userName
const loginIdentifier = 'admin'; // hoặc 'admin@example.com'

const response = await fetch('http://localhost:5000/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    userName: loginIdentifier, // Có thể là username hoặc email
    password: 'password123'
  })
});

const data = await response.json();
if (response.ok) {
  // Lưu token
  localStorage.setItem('token', data.token);
  console.log('User:', data.user);
  console.log('Roles:', data.user.roles);
} else {
  console.error('Login failed:', data);
}
```

**Ví dụ với form:**
```javascript
async function handleLogin(event) {
  event.preventDefault();
  
  const formData = new FormData(event.target);
  const loginValue = formData.get('login'); // Có thể là username hoặc email
  
  const response = await fetch('http://localhost:5000/api/auth/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      userName: loginValue, // Tự động nhận diện là username hay email
      password: formData.get('password')
    })
  });
  
  const data = await response.json();
  if (response.ok) {
    localStorage.setItem('token', data.token);
    // Redirect hoặc update UI
  } else {
    alert('Đăng nhập thất bại: ' + (data.detail || 'Sai tên đăng nhập/email hoặc mật khẩu'));
  }
}
```

### Postman Example

1. **Method:** `POST`
2. **URL:** `http://localhost:5000/api/auth/login`
3. **Headers:**
   - `Content-Type`: `application/json`
4. **Body** (raw JSON):
```json
{
  "userName": "admin",  // Có thể là username hoặc email
  "password": "password123"
}
```

**Lưu ý:** Trường `userName` có thể nhận:
- Username (ví dụ: `"admin"`, `"user123"`)
- Email (ví dụ: `"admin@example.com"`, `"user123@gmail.com"`)

5. **Tests Tab** (để tự động lưu token):
```javascript
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    pm.environment.set("jwt_token", jsonData.token);
    pm.environment.set("user_id", jsonData.user.userId);
    pm.environment.set("user_name", jsonData.user.userName);
    console.log("Token saved:", jsonData.token);
}
```

---

## 🔥 2. Đăng Nhập với Firebase ID Token (Email/Password)

### Endpoint
```
POST /api/auth/login/firebase-token
```

### Authorization
**Không cần** - Endpoint này public (`[AllowAnonymous]`)

### Headers
```
Content-Type: application/json
```

### Request Body

**Schema:**
```json
{
  "idToken": "string (required)"
}
```

**Ví dụ:**
```json
{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjE2MzQ1Njc4OTAiLCJ0eXAiOiJKV1QifQ..."
}
```

### Response Success (200 OK)

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "userId": 5,
    "userName": "user",
    "fullName": "John Doe",
    "email": "user@example.com",
    "firebaseUID": "abc123xyz789",
    "roles": ["User"],
    "emailVerified": true
  }
}
```

### Response Error

#### 400 Bad Request - Thiếu IdToken
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "IdToken is required"
}
```

#### 401 Unauthorized - Token không hợp lệ
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid Firebase ID token: {error_message}"
}
```

### Behavior

- **Verify Firebase ID Token:** Backend verify token với Firebase Admin SDK
- **Tự động tạo user:** Nếu user chưa tồn tại, tự động tạo với:
  - Email từ Firebase token
  - Name từ Firebase token
  - FirebaseUID từ token
  - Gán role mặc định "User"
- **Đồng bộ thông tin:** Tự động cập nhật email/name nếu có thay đổi
- **Custom Claims:** Lấy roles từ Firebase custom claims (nếu có)

### Flow hoàn chỉnh

1. **Frontend login với Firebase:**
```javascript
// Angular/React example
import { signInWithEmailAndPassword } from 'firebase/auth';

const userCredential = await signInWithEmailAndPassword(auth, email, password);
const idToken = await userCredential.user.getIdToken();
```

2. **Gửi ID Token lên backend:**
```javascript
const response = await fetch('http://localhost:5000/api/auth/login/firebase-token', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    idToken: idToken
  })
});

const data = await response.json();
// Lưu JWT token từ backend
localStorage.setItem('token', data.token);
```

### cURL Example
```bash
curl -X POST "http://localhost:5000/api/auth/login/firebase-token" \
  -H "Content-Type: application/json" \
  -d '{
    "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjE2MzQ1Njc4OTAiLCJ0eXAiOiJKV1QifQ..."
  }'
```

### JavaScript Example (Frontend)
```javascript
// 1. Login với Firebase
import { signInWithEmailAndPassword, getAuth } from 'firebase/auth';

const auth = getAuth();
const userCredential = await signInWithEmailAndPassword(
  auth, 
  'user@example.com', 
  'password123'
);

// 2. Lấy ID Token
const idToken = await userCredential.user.getIdToken();

// 3. Gửi lên backend để lấy JWT token
const response = await fetch('http://localhost:5000/api/auth/login/firebase-token', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ idToken })
});

const data = await response.json();
if (response.ok) {
  // Lưu JWT token từ backend
  localStorage.setItem('token', data.token);
  console.log('User:', data.user);
} else {
  console.error('Login failed:', data);
}
```

### Postman Example

1. **Method:** `POST`
2. **URL:** `http://localhost:5000/api/auth/login/firebase-token`
3. **Headers:**
   - `Content-Type`: `application/json`
4. **Body** (raw JSON):
```json
{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjE2MzQ1Njc4OTAiLCJ0eXAiOiJKV1QifQ..."
}
```

**Lưu ý:** `idToken` phải là token hợp lệ từ Firebase Authentication (sau khi user login với email/password trên Firebase).

---

## 🔥 3. Đăng Nhập với Firebase UID

### Endpoint
```
POST /api/auth/login/firebase
```

### Authorization
**Không cần** - Endpoint này public (`[AllowAnonymous]`)

### Headers
```
Content-Type: application/json
```

### Request Body

**Schema:**
```json
{
  "firebaseUID": "string (required)",
  "email": "string (optional)",
  "fullName": "string (optional)"
}
```

**Ví dụ:**
```json
{
  "firebaseUID": "abc123xyz789",
  "email": "user@example.com",
  "fullName": "John Doe"
}
```

### Response Success (200 OK)

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "userId": 5,
    "userName": "user",
    "fullName": "John Doe",
    "email": "user@example.com",
    "firebaseUID": "abc123xyz789",
    "roles": ["User"]
  }
}
```

### Response Error

#### 400 Bad Request - Thiếu FirebaseUID
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "FirebaseUID is required"
}
```

#### 401 Unauthorized - Tài khoản bị vô hiệu hóa
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "User account is inactive"
}
```

### Behavior

- **Nếu user chưa tồn tại:** Tự động tạo user mới với:
  - Username được tạo từ email (phần trước @) hoặc từ FirebaseUID
  - Gán role mặc định "User"
  - Tự động sync thông tin từ Firebase

- **Nếu user đã tồn tại:**
  - Cập nhật email và fullName nếu có thay đổi
  - Trả về token với roles hiện tại

### cURL Example
```bash
curl -X POST "http://localhost:5000/api/auth/login/firebase" \
  -H "Content-Type: application/json" \
  -d '{
    "firebaseUID": "abc123xyz789",
    "email": "user@example.com",
    "fullName": "John Doe"
  }'
```

---

## 👤 3. Lấy Thông Tin User Hiện Tại

### Endpoint
```
GET /api/auth/me
```

### Authorization
**Cần** - JWT Token (`[Authorize]`)

### Headers
```
Authorization: Bearer {JWT_TOKEN}
```

### Response Success (200 OK)

```json
{
  "name": "admin",
  "roles": ["Admin", "User"],
  "permissions": ["files.view", "files.manage", "users.manage"]
}
```

### Response Error

#### 401 Unauthorized - Token không hợp lệ hoặc thiếu
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "The request is unauthenticated. Pass the correct auth credentials."
}
```

### cURL Example
```bash
curl -X GET "http://localhost:5000/api/auth/me" \
  -H "Authorization: Bearer {YOUR_JWT_TOKEN}"
```

### JavaScript Example
```javascript
const token = localStorage.getItem('token');
const response = await fetch('http://localhost:5000/api/auth/me', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

const userInfo = await response.json();
console.log('Current user:', userInfo);
```

---

## 🔑 JWT Token Details

### Token Structure

Token chứa các claims:
- `sub`: UserId (JwtRegisteredClaimNames.Sub)
- `name`: UserName (ClaimTypes.Name)
- `role`: Danh sách roles (có thể có nhiều) - ClaimTypes.Role
- `permission`: Danh sách permissions - "permission"

### Token Configuration

Từ `appsettings.json`:
```json
{
  "Jwt": {
    "Issuer": "quanlyfiles",
    "Audience": "quanlyfiles-client",
    "Key": "CHANGE_THIS_DEVELOPMENT_SECRET_KEY_32_CHARS_MIN",
    "ExpiryMinutes": 120
  }
}
```

- **Issuer:** `quanlyfiles`
- **Audience:** `quanlyfiles-client`
- **Expiry:** 120 phút (2 giờ)
- **Algorithm:** HS256 (HMAC SHA256)

### Sử dụng Token

Sau khi login, sử dụng token trong header:
```
Authorization: Bearer {token}
```

**Lưu ý:**
- Phải có từ "Bearer " (với dấu space) trước token
- Token có thời hạn 120 phút, cần đăng nhập lại khi hết hạn

---

## 📝 Ví Dụ Flow Hoàn Chỉnh

### Bước 1: Đăng nhập
```bash
POST /api/auth/login
Body: {
  "userName": "admin",
  "password": "password123"
}

Response: {
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": { ... }
}
```

### Bước 2: Lưu token
```javascript
localStorage.setItem('token', data.token);
```

### Bước 3: Sử dụng token cho các API khác
```bash
GET /api/users/test-auth
Headers: {
  "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

---

## ⚠️ Lưu Ý Quan Trọng

1. **Password Hash:** Hệ thống sử dụng BCrypt để hash password
2. **User Active:** User phải có `IsActive = true` mới có thể đăng nhập
3. **Roles & Permissions:** Token chứa roles và permissions từ database
4. **Firebase Login:** Tự động tạo user nếu chưa tồn tại với role "User" mặc định
5. **Token Expiry:** Token hết hạn sau 120 phút, cần đăng nhập lại

---

## 🧪 Testing trong Postman

### Collection Setup

1. Tạo Environment Variables:
   - `base_url`: `http://localhost:5000`
   - `jwt_token`: (sẽ được set tự động sau login)

2. Request: Login
   - URL: `{{base_url}}/api/auth/login`
   - Tests script để lưu token (xem ở trên)

3. Request: Me
   - URL: `{{base_url}}/api/auth/me`
   - Authorization: Bearer Token `{{jwt_token}}`

4. Request: Create User (cần Admin role)
   - URL: `{{base_url}}/api/users`
   - Authorization: Bearer Token `{{jwt_token}}`

---

## 🔍 Debug Tips

### Kiểm tra token có hợp lệ:
```bash
GET /api/auth/me
Authorization: Bearer {token}
```

### Kiểm tra roles trong token:
```bash
GET /api/users/test-auth
Authorization: Bearer {token}
```

### Xem token decoded:
Sử dụng [jwt.io](https://jwt.io) để decode token và xem claims

---

## 📞 Support

Nếu gặp lỗi:
1. Kiểm tra server đang chạy
2. Kiểm tra username/password đúng
3. Kiểm tra user có `IsActive = true`
4. Kiểm tra token format: `Bearer {token}` (có space)
5. Kiểm tra console logs của server

