# Hướng dẫn Convert PersonConfirmation từ nvarchar sang bit

## Vấn đề hiện tại
- Database: `PersonConfirmation` là `nvarchar(50)` (string)
- Model C#: `PersonConfirmation` là `bool?` (boolean)
- Gây ra lỗi `InvalidCastException` khi EF Core cố cast string sang bool

## Giải pháp: Update Database Schema

### Cách 1: Chạy SQL Script trực tiếp (Khuyến nghị)

1. **Backup database trước khi chạy:**
   ```sql
   BACKUP DATABASE [YourDatabaseName] 
   TO DISK = 'C:\Backup\YourDatabaseName.bak'
   ```

2. **Chạy script migration:**
   - Mở SQL Server Management Studio
   - Connect đến database
   - Mở file `ConvertPersonConfirmationToBit.sql`
   - Execute script

3. **Verify kết quả:**
   - Kiểm tra schema: `PersonConfirmation` phải là `bit`
   - Kiểm tra dữ liệu: các giá trị phải là 0, 1, hoặc NULL

### Cách 2: Tạo EF Core Migration (Nếu muốn dùng migration)

1. **Update ApplicationDbContext.cs:**
   ```csharp
   entity.Property(e => e.PersonConfirmation)
       .HasColumnType("bit");
   ```

2. **Tạo migration:**
   ```bash
   dotnet ef migrations add ConvertPersonConfirmationToBit
   ```

3. **Chỉnh sửa migration file:**
   - Mở file migration vừa tạo
   - Thay thế `AlterColumn` bằng script convert dữ liệu

4. **Apply migration:**
   ```bash
   dotnet ef database update
   ```

## Lưu ý quan trọng

1. **Backup database trước khi chạy migration**
2. **Kiểm tra dữ liệu hiện có:**
   ```sql
   SELECT DISTINCT PersonConfirmation, COUNT(*) 
   FROM WorkItem 
   GROUP BY PersonConfirmation;
   ```
3. **Test trên môi trường dev trước**
4. **Sau khi update, có thể xóa code conversion trong controller**

## Sau khi update database

Sau khi update database thành công, bạn có thể:
1. Xóa code raw SQL conversion trong `WorkItemsController.cs`
2. Xóa code raw SQL conversion trong `AssignmentsController.cs`
3. Sử dụng EF Core bình thường:
   ```csharp
   var workItem = await _context.WorkItems
       .FirstOrDefaultAsync(wi => wi.WorkItemID == id);
   // PersonConfirmation sẽ tự động cast đúng
   ```

## Rollback (Nếu cần)

Nếu cần rollback, chạy script sau:
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

