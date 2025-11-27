# Cách Xem Logs Console của Backend

## 1. Logs hiển thị trong Terminal/Console

Khi bạn chạy backend bằng `dotnet run`, **logs sẽ hiển thị trực tiếp trong terminal/console** nơi bạn chạy lệnh.

### Cách chạy và xem logs:

#### Option 1: Chạy trực tiếp
```powershell
cd d:\Project\thibidi\quanlyfiles\quanlyfilesBE
dotnet run
```

**Logs sẽ hiển thị ngay trong cửa sổ PowerShell này:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
[Routing Debug] GET /swagger
[Routing Debug] PUT /api/work-items/66
UpdateWorkItem called with ID: 66, DTO: {...}
```

#### Option 2: Dùng script rebuild-and-run.ps1
```powershell
.\rebuild-and-run.ps1
```

Script này sẽ tự động build và chạy, logs sẽ hiển thị trong cùng cửa sổ.

## 2. Các loại logs bạn sẽ thấy

### a) Logs khi khởi động:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### b) Logs routing (từ middleware):
```
[Routing Debug] PUT /api/work-items/66
[CORS Debug] Method: PUT, Origin: http://localhost:4200, Path: /api/work-items/66
```

### c) Logs từ controller:
```
info: quanlyfilesBE.Controllers.WorkItemsController[0]
      UpdateWorkItem called with ID: 66, DTO: {...}
info: quanlyfilesBE.Controllers.WorkItemsController[0]
      Total work items in database: 5
info: quanlyfilesBE.Controllers.WorkItemsController[0]
      All work item IDs in database: [66, 67, 68, 69, 70]
```

### d) Logs từ Console.WriteLine (từ middleware):
```
[Routing Debug] PUT /api/work-items/66
```

## 3. Cách xem logs khi đang chạy

### Nếu backend đang chạy trong một cửa sổ PowerShell:
1. **Mở cửa sổ PowerShell đó** (nơi bạn chạy `dotnet run`)
2. **Scroll lên/xuống** để xem logs cũ
3. **Logs mới sẽ tự động hiển thị** khi có request

### Nếu muốn lưu logs vào file:
```powershell
dotnet run | Tee-Object -FilePath "logs.txt"
```

Hoặc redirect toàn bộ output:
```powershell
dotnet run > logs.txt 2>&1
```

## 4. Kiểm tra logs khi test API

### Bước 1: Đảm bảo backend đang chạy
- Mở PowerShell
- Chạy: `cd d:\Project\thibidi\quanlyfiles\quanlyfilesBE`
- Chạy: `dotnet run`
- **Giữ cửa sổ này mở** để xem logs

### Bước 2: Test từ frontend hoặc browser
- Gọi API PUT `/api/work-items/66` từ frontend
- **Ngay lập tức** quay lại cửa sổ PowerShell
- Xem logs mới xuất hiện

### Bước 3: Tìm logs liên quan
Tìm các dòng có chứa:
- `[Routing Debug]` - Request đã đến routing
- `UpdateWorkItem called` - Request đã đến controller
- `Total work items` - Database query đã chạy
- `Work item with ID 66 not found` - Không tìm thấy work item

## 5. Ví dụ logs khi có lỗi 404

### Nếu request KHÔNG đến controller:
```
[Routing Debug] PUT /api/work-items/66
(SAU ĐÓ KHÔNG CÓ LOGS NÀO KHÁC)
```
→ Có thể routing không match hoặc authorization chặn

### Nếu request đến controller nhưng không tìm thấy:
```
[Routing Debug] PUT /api/work-items/66
info: quanlyfilesBE.Controllers.WorkItemsController[0]
      UpdateWorkItem called with ID: 66
info: quanlyfilesBE.Controllers.WorkItemsController[0]
      Total work items in database: 5
info: quanlyfilesBE.Controllers.WorkItemsController[0]
      All work item IDs in database: [67, 68, 69, 70]  ← KHÔNG CÓ 66!
warn: quanlyfilesBE.Controllers.WorkItemsController[0]
      Work item with ID 66 not found
```
→ Work item ID 66 không tồn tại trong database

### Nếu request thành công:
```
[Routing Debug] PUT /api/work-items/66
info: quanlyfilesBE.Controllers.WorkItemsController[0]
      UpdateWorkItem called with ID: 66
info: quanlyfilesBE.Controllers.WorkItemsController[0]
      Total work items in database: 5
info: quanlyfilesBE.Controllers.WorkItemsController[0]
      All work item IDs in database: [66, 67, 68, 69, 70]
info: quanlyfilesBE.Controllers.WorkItemsController[0]
      Successfully updated work item 66
```

## 6. Tips

### Xem logs real-time:
- **Không đóng** cửa sổ PowerShell nơi backend đang chạy
- Logs sẽ tự động scroll xuống khi có request mới

### Tìm logs cụ thể:
- Trong PowerShell, dùng `Ctrl+F` để tìm kiếm
- Tìm theo từ khóa: `UpdateWorkItem`, `66`, `Work item`, etc.

### Clear console:
- Trong PowerShell: `Clear-Host` hoặc `cls`
- Hoặc đóng và mở lại terminal

### Nếu không thấy logs:
1. Đảm bảo backend đang chạy
2. Kiểm tra xem có rebuild backend sau khi sửa code không
3. Kiểm tra log level trong `appsettings.json` (phải là "Information" hoặc "Debug")

## 7. Cấu hình log level

Trong `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",  // Hiển thị Information, Warning, Error
      "Microsoft.AspNetCore": "Warning"  // Chỉ hiển thị Warning trở lên
    }
  }
}
```

Để xem nhiều logs hơn, đổi thành:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",  // Hiển thị tất cả logs
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```


