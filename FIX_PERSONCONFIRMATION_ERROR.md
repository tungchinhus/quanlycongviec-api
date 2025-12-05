# Hướng dẫn sửa lỗi PersonConfirmation InvalidCastException

## Vấn đề

Lỗi `System.InvalidCastException: Unable to cast object of type 'System.String' to type 'System.Boolean'` xảy ra vì:

- **Database**: Cột `PersonConfirmation` trong bảng `WorkItem` là `nvarchar(50)` (string)
- **Model C#**: Property `PersonConfirmation` trong class `WorkItem` là `bool?` (boolean)
- **Nguyên nhân**: Entity Framework Core không thể tự động convert string sang boolean khi đọc từ database

## Giải pháp

Cần chuyển đổi cột `PersonConfirmation` từ `nvarchar(50)` sang `bit` trong database.

## Cách 1: Sử dụng PowerShell Script (Khuyến nghị)

### Bước 1: Kiểm tra trạng thái hiện tại

```powershell
.\check-personconfirmation-type.ps1
```

Script này sẽ:
- Kiểm tra type hiện tại của cột `PersonConfirmation`
- Hiển thị sample data
- Cho biết có cần migration hay không

### Bước 2: Chạy migration

```powershell
.\fix-personconfirmation-migration.ps1
```

Script này sẽ:
- Tự động backup database (trừ khi dùng `-SkipBackup`)
- Chuyển đổi cột từ `nvarchar(50)` sang `bit`
- Convert dữ liệu hiện có (string -> boolean)
- Verify kết quả

**Lưu ý**: Script sẽ tự động backup database trước khi chạy migration. Nếu muốn bỏ qua backup:

```powershell
.\fix-personconfirmation-migration.ps1 -SkipBackup
```

### Bước 3: Verify lại

```powershell
.\check-personconfirmation-type.ps1
```

Bạn sẽ thấy: `✓ Status: OK - Column is BIT type (correct)`

## Cách 2: Chạy SQL Script thủ công

### Bước 1: Backup database

```sql
BACKUP DATABASE [quanlyphancong] 
TO DISK = 'C:\Backup\quanlyphancong.bak'
```

### Bước 2: Mở SQL Server Management Studio

1. Connect đến SQL Server
2. Mở file `Scripts\ConvertPersonConfirmationToBit.sql`
3. Execute script (F5)

### Bước 3: Verify

```sql
SELECT 
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'WorkItem' 
AND COLUMN_NAME = 'PersonConfirmation'
```

Kết quả phải là: `DATA_TYPE = 'bit'`

## Cách 3: Sử dụng EF Core Migration

Nếu bạn muốn dùng EF Core migration:

```bash
# Tạo migration mới
dotnet ef migrations add ConvertPersonConfirmationToBit

# Chỉnh sửa file migration để thêm logic convert dữ liệu
# (Copy từ Scripts/ConvertPersonConfirmationToBit.sql)

# Apply migration
dotnet ef database update
```

## Sau khi migration

1. **Restart application**: Khởi động lại ứng dụng để áp dụng thay đổi
2. **Test API**: Thử update work item để verify lỗi đã được fix
3. **Code đã được sửa**: Controller `WorkItemsController` đã được update để sử dụng EF Core thay vì raw SQL

## Rollback (Nếu cần)

Nếu cần rollback về `nvarchar`, chạy script sau:

```sql
-- Add back nvarchar column
ALTER TABLE WorkItem
ADD PersonConfirmation_NVarChar NVARCHAR(50) NULL;

-- Convert bit back to string
UPDATE WorkItem
SET PersonConfirmation_NVarChar = 
    CASE 
        WHEN PersonConfirmation IS NULL THEN NULL
        WHEN PersonConfirmation = 1 THEN 'true'
        WHEN PersonConfirmation = 0 THEN 'false'
    END;

-- Drop bit column
ALTER TABLE WorkItem
DROP COLUMN PersonConfirmation;

-- Rename
EXEC sp_rename 'WorkItem.PersonConfirmation_NVarChar', 'PersonConfirmation', 'COLUMN';
```

## Kiểm tra dữ liệu hiện có

Trước khi migration, bạn có thể kiểm tra dữ liệu hiện có:

```sql
SELECT DISTINCT PersonConfirmation, COUNT(*) 
FROM WorkItem 
GROUP BY PersonConfirmation;
```

## Troubleshooting

### Lỗi: "Column already exists"
- Script đã được chạy trước đó
- Kiểm tra bằng `check-personconfirmation-type.ps1`

### Lỗi: "Cannot drop column because it is referenced"
- Có thể có constraint hoặc index
- Kiểm tra và drop constraint/index trước

### Lỗi: "Permission denied"
- Đảm bảo user có quyền ALTER TABLE
- Chạy với quyền admin hoặc sa

## Files liên quan

- `Scripts/ConvertPersonConfirmationToBit.sql` - SQL migration script
- `fix-personconfirmation-migration.ps1` - PowerShell script để chạy migration
- `check-personconfirmation-type.ps1` - PowerShell script để kiểm tra column type
- `Controllers/WorkItemsController.cs` - Controller đã được fix
- `Data/ApplicationDbContext.cs` - DbContext đã được cấu hình cho `bit` type

