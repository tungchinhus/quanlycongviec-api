# Database Scripts

## CreateDatabase.sql

Script này tạo database và tất cả các bảng cần thiết cho hệ thống Quản lý Files.

### Cách chạy script:

#### Option 1: Sử dụng SQL Server Management Studio (SSMS)
1. Mở SQL Server Management Studio
2. Kết nối đến SQL Server instance của bạn
3. Mở file `CreateDatabase.sql`
4. Chạy script (F5 hoặc Execute)

#### Option 2: Sử dụng sqlcmd command line
```bash
sqlcmd -S localhost -E -i Scripts/CreateDatabase.sql
```

#### Option 3: Sử dụng Azure Data Studio
1. Mở Azure Data Studio
2. Kết nối đến SQL Server
3. Mở file `CreateDatabase.sql`
4. Chạy script

### Cấu trúc Database:

1. **Users** - Lưu thông tin người dùng
   - UserId (PK)
   - UserName (Unique)
   - FirebaseUID (Unique, nullable)
   - Email, FullName, IsActive

2. **Roles** - Các vai trò trong hệ thống
   - RoleId (PK)
   - RoleName (Unique)
   - Description

3. **Permissions** - Các quyền trong hệ thống
   - PermissionId (PK)
   - PermissionName (Unique)
   - Description

4. **UserRoles** - Quan hệ nhiều-nhiều giữa Users và Roles
   - UserId (FK)
   - RoleId (FK)
   - AssignedAt

5. **RolePermissions** - Quan hệ nhiều-nhiều giữa Roles và Permissions
   - RoleId (FK)
   - PermissionId (FK)

6. **Folders** - Thư mục
   - Id (PK)
   - FolderName, FolderPath
   - ParentFolderId (Self-referencing)

7. **Files** - File
   - Id (PK)
   - FileName, FilePath, FileType
   - FileSize, UploadDate, UploadedBy

### Dữ liệu mẫu được tạo:

- **Roles**: Admin, User
- **Permissions**: files.view, files.manage, users.manage
- **RolePermissions**: Admin có tất cả permissions

## UpdateUserNames.sql

Script này cập nhật tên (FullName) của user trong database để đồng bộ với Firebase Authentication custom claims.

### Cách sử dụng:

1. Mở file `UpdateUserNames.sql` trong SQL Server Management Studio
2. Kiểm tra và chỉnh sửa email và tên tương ứng nếu cần
3. Chạy script (F5 hoặc Execute)

### Script hiện tại cập nhật:

- `tung.lm@thibidi.com` → `Lê Minh Tùng`
- `hoa.dc@thibidi.com` → `Dương Công Hòa`

### Lưu ý:

- Script sẽ hiển thị thông báo nếu không tìm thấy user với email tương ứng
- Script sẽ hiển thị thông tin user sau khi cập nhật thành công
- Có thể mở rộng script để thêm nhiều user khác nếu cần

### Lưu ý:

- Admin user sẽ được tạo tự động bởi `DbSeeder` khi ứng dụng khởi động lần đầu
- Username mặc định: `admin`
- Password mặc định: `Admin@123`
- Nếu database đã tồn tại, script sẽ bỏ qua các bảng đã có (không xóa dữ liệu cũ)

