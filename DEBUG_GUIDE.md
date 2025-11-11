# Hướng Dẫn Debug trong Visual Studio / VS Code

## 🎯 Debug tại dòng 136 trong AuthController.cs

Dòng 136 nằm trong method `LoginWithFirebase` - đây là nơi kiểm tra xem user có tồn tại trong database hay không.

---

## 📋 Cách 1: Debug trong Visual Studio

### Bước 1: Đặt Breakpoint

1. Mở file `Controllers/AuthController.cs`
2. Click vào **lề trái** (gutter) tại dòng 136, hoặc đặt con trỏ tại dòng 136 và nhấn **F9**
3. Bạn sẽ thấy một **chấm đỏ** xuất hiện - đó là breakpoint

### Bước 2: Chạy ứng dụng ở chế độ Debug

1. Nhấn **F5** hoặc click **Start Debugging** (▶️ với biểu tượng bug)
2. Hoặc chọn menu: **Debug > Start Debugging**
3. Ứng dụng sẽ chạy và tự động mở Swagger tại `http://localhost:5000/swagger`

### Bước 3: Trigger Breakpoint

Gửi request đến endpoint `/api/auth/login/firebase`:

**Cách 1: Dùng Swagger UI**
- Mở `http://localhost:5000/swagger`
- Tìm endpoint `POST /api/auth/login/firebase`
- Click **Try it out**
- Nhập body:
```json
{
  "firebaseUID": "test-uid-123",
  "email": "test@example.com",
  "fullName": "Test User"
}
```
- Click **Execute**

**Cách 2: Dùng Postman**
- POST `http://localhost:5000/api/auth/login/firebase`
- Headers: `Content-Type: application/json`
- Body (raw JSON):
```json
{
  "firebaseUID": "test-uid-123",
  "email": "test@example.com",
  "fullName": "Test User"
}
```

### Bước 4: Sử dụng Debugger

Khi breakpoint được hit, bạn có thể:

1. **Xem giá trị biến:**
   - Hover chuột lên biến để xem giá trị
   - Xem trong cửa sổ **Locals** (hiển thị tất cả biến local)
   - Xem trong cửa sổ **Watch** (thêm biến muốn theo dõi)

2. **Điều khiển thực thi:**
   - **F10** (Step Over): Chạy dòng hiện tại, không vào function
   - **F11** (Step Into): Vào bên trong function
   - **Shift+F11** (Step Out): Thoát khỏi function hiện tại
   - **F5** (Continue): Tiếp tục chạy đến breakpoint tiếp theo

3. **Xem Call Stack:**
   - Cửa sổ **Call Stack** hiển thị chuỗi function calls dẫn đến breakpoint

4. **Xem Immediate Window:**
   - Nhấn **Ctrl+Alt+I** để mở Immediate Window
   - Gõ biểu thức để đánh giá ngay: `req.FirebaseUID`, `user?.UserId`, etc.

---

## 📋 Cách 2: Debug trong VS Code

### Bước 1: Tạo file launch.json

Tạo file `.vscode/launch.json` (nếu chưa có):

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/bin/Debug/net9.0/quanlyfilesBE.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    }
  ]
}
```

### Bước 2: Tạo file tasks.json

Tạo file `.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/quanlyfilesBE.csproj",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary"
      ],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

### Bước 3: Đặt Breakpoint và Debug

1. Click vào lề trái tại dòng 136 để đặt breakpoint
2. Nhấn **F5** hoặc click **Run and Debug** (▶️)
3. Chọn configuration ".NET Core Launch (web)"
4. Gửi request như hướng dẫn ở trên

---

## 🔍 Các biến quan trọng cần kiểm tra tại dòng 136

Khi debug tại dòng 136, bạn nên kiểm tra:

1. **`req`** - Request object:
   - `req.FirebaseUID` - Firebase UID từ client
   - `req.Email` - Email (nếu có)
   - `req.FullName` - Tên đầy đủ (nếu có)

2. **`user`** - User từ database:
   - Nếu `user == null`: User chưa tồn tại, sẽ tạo mới
   - Nếu `user != null`: User đã tồn tại, sẽ cập nhật thông tin

3. **`_db.Users`** - Database context:
   - Kiểm tra connection string
   - Kiểm tra query được thực thi

---

## 🛠️ Debug với Logging

Nếu không thể dùng debugger, bạn có thể thêm logging:

### Thêm logging tại dòng 136:

```csharp
_logger?.LogInformation("LoginWithFirebase - Checking user existence. FirebaseUID: {FirebaseUID}", req.FirebaseUID);
var user = await _db.Users
    .Include(u => u.UserRoles)
    .ThenInclude(ur => ur.Role)
    .ThenInclude(r => r.RolePermissions)
    .ThenInclude(rp => rp.Permission)
    .FirstOrDefaultAsync(u => u.FirebaseUID == req.FirebaseUID);

_logger?.LogInformation("LoginWithFirebase - User lookup result: {UserExists}, UserId: {UserId}", 
    user != null, user?.UserId);
```

Xem logs trong:
- **Visual Studio**: Output window (View > Output)
- **VS Code**: Debug Console
- **Terminal**: Nếu chạy bằng `dotnet run`

---

## 🐛 Common Debug Scenarios

### Scenario 1: User không tồn tại (user == null)

**Kiểm tra:**
- `req.FirebaseUID` có giá trị không?
- Database có kết nối không?
- Query có đúng không?

**Debug:**
```csharp
// Thêm vào trước dòng 136
_logger?.LogInformation("FirebaseUID from request: {FirebaseUID}", req.FirebaseUID);
var allUsers = await _db.Users.Select(u => new { u.UserId, u.FirebaseUID }).ToListAsync();
_logger?.LogInformation("All users in DB: {Users}", string.Join(", ", allUsers));
```

### Scenario 2: User tồn tại nhưng không có roles

**Kiểm tra:**
- `user.UserRoles` có null không?
- `user.UserRoles.Count()` là bao nhiêu?

**Debug:**
```csharp
// Thêm vào sau dòng 186 (sau khi reload user)
_logger?.LogInformation("User roles count: {Count}", user.UserRoles?.Count() ?? 0);
```

### Scenario 3: Database connection issues

**Kiểm tra:**
- Connection string trong `appsettings.json`
- Database server có đang chạy không?
- Test connection: `GET http://localhost:5000/api/test-db`

---

## 📝 Tips Debugging

1. **Conditional Breakpoints:**
   - Right-click breakpoint > Conditions
   - Đặt điều kiện: `req.FirebaseUID == "specific-uid"`

2. **Logpoints:**
   - Thay vì breakpoint, dùng logpoint để log mà không dừng execution
   - Right-click breakpoint > Edit Breakpoint > Logpoint

3. **Watch Expressions:**
   - Thêm vào Watch window:
     - `user?.UserId`
     - `user?.UserRoles?.Count()`
     - `req.FirebaseUID`

4. **Immediate Window:**
   - Đánh giá biểu thức ngay: `_db.Users.Count()`
   - Thay đổi giá trị: `req.FirebaseUID = "new-value"`

---

## 🚀 Quick Start

1. Đặt breakpoint tại dòng 136
2. Nhấn F5 để chạy debug
3. Gửi POST request đến `/api/auth/login/firebase` với body:
```json
{
  "firebaseUID": "test-debug-123"
}
```
4. Debugger sẽ dừng tại dòng 136
5. Hover chuột lên `user` để xem giá trị
6. Nhấn F10 để step over từng dòng

---

## 📞 Troubleshooting

**Breakpoint không được hit:**
- Đảm bảo đang chạy ở chế độ Debug (F5), không phải Run (Ctrl+F5)
- Kiểm tra code đã được build chưa
- Kiểm tra breakpoint có được enable không (không bị disabled - chấm trắng)

**Không thấy biến trong Locals:**
- Đảm bảo đang ở đúng scope (trong method `LoginWithFirebase`)
- Kiểm tra code đã được compile với debug symbols

**Database query không trả về kết quả:**
- Test connection: `GET /api/test-db`
- Kiểm tra logs để xem có exception không
- Thử query trực tiếp trong database

