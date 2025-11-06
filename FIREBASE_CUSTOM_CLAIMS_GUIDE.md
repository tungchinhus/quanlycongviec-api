# Hướng dẫn kiểm tra Custom Claims của Firebase Users

Có nhiều cách để kiểm tra custom claims của các user trên Firebase. Dưới đây là các phương pháp:

## 1. Qua API Endpoints (Backend)

### 1.1. Kiểm tra Custom Claims theo FirebaseUID

**Endpoint:** `GET /api/users/check-custom-claims/{firebaseUid}`

**Ví dụ:**
```http
GET /api/users/check-custom-claims/snqkhI6JsDOJfuSy11t6wVgCY
```

**Response:**
```json
{
  "firebaseUID": "snqkhI6JsDOJfuSy11t6wVgCY",
  "firebaseUser": {
    "uid": "snqkhI6JsDOJfuSy11t6wVgCY",
    "email": "chinhdvt@gmail.com",
    "displayName": "System Administrator",
    "emailVerified": true,
    "disabled": false,
    "creationTime": "2024-01-01T00:00:00Z",
    "lastSignInTime": "2024-01-01T00:00:00Z"
  },
  "customClaims": {
    "roles": ["Admin"],
    "name": "System Administrator"
  },
  "rolesFromFirebase": ["Admin"],
  "rolesFromLocalDB": ["Admin"],
  "rolesMatch": true,
  "localUser": {
    "userId": 3,
    "userName": "chinhdvt1",
    "fullName": "System Administrator",
    "email": "chinhdvt@gmail.com",
    "isActive": true,
    "roles": ["Admin"]
  },
  "hasLocalUser": true
}
```

### 1.2. Kiểm tra Custom Claims theo Email

**Endpoint:** `GET /api/users/check-custom-claims-by-email?email={email}`

**Ví dụ:**
```http
GET /api/users/check-custom-claims-by-email?email=chinhdvt@gmail.com
```

### 1.3. Kiểm tra tất cả Custom Claims của tất cả users

**Endpoint:** `GET /api/users/load-all-custom-claims` (Yêu cầu Admin role)

**Ví dụ:**
```http
GET /api/users/load-all-custom-claims
Authorization: Bearer {token}
```

**Response:**
```json
{
  "totalFirebaseUsers": 10,
  "totalLocalUsers": 8,
  "usersWithClaims": 5,
  "users": [
    {
      "firebaseUID": "snqkhI6JsDOJfuSy11t6wVgCY",
      "email": "chinhdvt@gmail.com",
      "displayName": "System Administrator",
      "customClaims": {
        "roles": ["Admin"],
        "name": "System Administrator"
      },
      "localUser": {
        "userId": 3,
        "userName": "chinhdvt1",
        "fullName": "System Administrator",
        "isActive": true,
        "roles": ["Admin"]
      },
      "hasLocalUser": true
    }
  ]
}
```

## 2. Qua Firebase Console (Web UI)

### Cách 1: Firebase Authentication Console

1. Truy cập [Firebase Console](https://console.firebase.google.com/)
2. Chọn project của bạn
3. Vào **Authentication** → **Users**
4. Tìm user cần kiểm tra
5. Click vào user để xem chi tiết
6. Scroll xuống phần **Custom claims** để xem các claims

**Lưu ý:** Firebase Console không hiển thị custom claims trực tiếp trong UI. Bạn cần sử dụng Firebase Admin SDK hoặc API để xem.

### Cách 2: Firebase CLI

Nếu bạn có Firebase CLI được cài đặt:

```bash
# Xem custom claims của một user
firebase auth:export users.json
# Sau đó tìm user trong file JSON và xem customClaims
```

## 3. Qua Code (Backend)

### Sử dụng FirebaseService trong code:

```csharp
// Lấy user từ Firebase
var firebaseUser = await _firebaseService.GetUserAsync(firebaseUid);

// Kiểm tra custom claims
if (firebaseUser?.CustomClaims != null)
{
    var customClaims = firebaseUser.CustomClaims;
    
    // Kiểm tra roles
    if (customClaims.ContainsKey("roles"))
    {
        var roles = customClaims["roles"];
        // Xử lý roles...
    }
}
```

### Kiểm tra trong Controller:

```csharp
[HttpGet("check-claims")]
public async Task<IActionResult> CheckClaims()
{
    var firebaseUid = "snqkhI6JsDOJfuSy11t6wVgCY";
    var firebaseUser = await _firebaseService.GetUserAsync(firebaseUid);
    
    if (firebaseUser?.CustomClaims != null)
    {
        return Ok(firebaseUser.CustomClaims);
    }
    
    return Ok(new { message = "No custom claims found" });
}
```

## 4. Qua ID Token (Frontend)

Khi user đăng nhập, ID token có thể chứa custom claims. Tuy nhiên, để custom claims xuất hiện trong ID token, user cần:

1. Đăng nhập lại sau khi custom claims được set
2. Hoặc token được refresh

### Trong Frontend (JavaScript):

```javascript
// Sau khi user đăng nhập
firebase.auth().currentUser.getIdTokenResult()
  .then((idTokenResult) => {
    // Kiểm tra custom claims
    console.log('Custom claims:', idTokenResult.claims);
    console.log('Roles:', idTokenResult.claims.roles);
  });
```

## 5. Debugging Tips

### Kiểm tra xem roles có khớp giữa Firebase và Local DB không:

Endpoint `check-custom-claims` sẽ trả về `rolesMatch: true/false` để so sánh:

- `rolesFromFirebase`: Roles từ Firebase Custom Claims
- `rolesFromLocalDB`: Roles từ bảng UserRoles trong database
- `rolesMatch`: Boolean cho biết 2 danh sách có khớp không

### Đồng bộ roles từ Firebase xuống Local DB:

Nếu roles không khớp, bạn có thể đồng bộ:

```http
POST /api/users/by-firebase-uid/{firebaseUid}/sync-roles
Authorization: Bearer {admin_token}
```

### Đồng bộ roles từ Local DB lên Firebase:

Khi update roles trong UsersController, nó sẽ tự động đồng bộ lên Firebase nếu user có FirebaseUID.

## 6. Troubleshooting

### Vấn đề: Custom claims không hiển thị trong ID token

**Giải pháp:**
- User cần đăng xuất và đăng nhập lại
- Hoặc refresh ID token: `firebase.auth().currentUser.getIdToken(true)`

### Vấn đề: Custom claims không khớp giữa Firebase và Local DB

**Giải pháp:**
- Sử dụng endpoint `sync-roles` để đồng bộ
- Hoặc kiểm tra xem có lỗi khi set custom claims không

### Vấn đề: Không thể xem custom claims trong Firebase Console

**Giải pháp:**
- Firebase Console không hiển thị custom claims trực tiếp
- Sử dụng API endpoints hoặc Firebase Admin SDK

## 7. Test với Postman/Thunder Client

### Test endpoint check-custom-claims:

```http
GET http://localhost:5000/api/users/check-custom-claims-by-email?email=chinhdvt@gmail.com
```

### Test endpoint load-all-custom-claims:

```http
GET http://localhost:5000/api/users/load-all-custom-claims
Authorization: Bearer {your_jwt_token}
```

## 8. Lưu ý quan trọng

1. **Custom claims phải được set bằng Admin SDK** - Không thể set từ client-side
2. **Custom claims có giới hạn kích thước** - Tối đa 1000 bytes
3. **Custom claims cần refresh token** - User phải đăng nhập lại hoặc refresh token
4. **Custom claims không tự động sync** - Cần code để đồng bộ giữa Firebase và Local DB

## 9. Tài liệu tham khảo

- [Firebase Custom Claims Documentation](https://firebase.google.com/docs/auth/admin/custom-claims)
- [Firebase Admin SDK for .NET](https://firebase.google.com/docs/reference/admin/dotnet/namespace/firebase-admin/auth)

