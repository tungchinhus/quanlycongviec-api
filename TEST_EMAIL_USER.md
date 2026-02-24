# User test chức năng email

Để test gửi/nhận email trong ứng dụng (Power Automate, thông báo, v.v.), dùng user test sau:

| Trường   | Giá trị                |
|----------|------------------------|
| **Email**   | `tungchinhus@gmail.com` |
| **Username**| `tungchinhus`           |
| **Mật khẩu mặc định** | `TestEmail123!` (có thể đổi sau khi đăng nhập) |

## Cách tạo user test

**Chạy script (backend phải đang chạy):**

```powershell
cd quanlyfilesBE
.\Scripts\create-test-email-user.ps1
```

Tùy chọn:

- Đổi mật khẩu: `.\Scripts\create-test-email-user.ps1 -Password "MatKhauCuaBan"`
- Backend khác port/host: `$env:QUANLYFILES_API_URL="http://localhost:5000"; .\Scripts\create-test-email-user.ps1`

Nếu user đã tồn tại (email hoặc username trùng), script sẽ báo "User test da ton tai" và thoát thành công.

## Tạo bằng API trực tiếp

```http
POST /api/users/create-simple
Content-Type: application/json

{
  "userName": "tungchinhus",
  "fullName": "User Test Email",
  "email": "tungchinhus@gmail.com",
  "password": "TestEmail123!",
  "roles": ["User"]
}
```

Sau khi tạo, đăng nhập bằng email `tungchinhus@gmail.com` để kiểm tra luồng email (giao việc, thông báo, v.v.).
