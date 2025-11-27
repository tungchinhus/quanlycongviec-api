# Hướng Dẫn Rebuild Backend

## Scripts có sẵn

### 1. `rebuild-and-run.ps1` - Rebuild và chạy ngay
Script này sẽ:
- Clean project
- Build lại project
- Chạy application với logging chi tiết

**Cách dùng:**
```powershell
.\rebuild-and-run.ps1
```

**Với watch mode (tự động reload khi có thay đổi file):**
```powershell
.\rebuild-and-run.ps1 -Watch
```

**Chỉ định port khác:**
```powershell
.\rebuild-and-run.ps1 -Port 5001
```

### 2. `rebuild-only.ps1` - Chỉ rebuild (không chạy)
Script này chỉ build lại project, không chạy application.

**Cách dùng:**
```powershell
.\rebuild-only.ps1
```

Sau đó chạy thủ công:
```powershell
dotnet run
```

## Sau khi rebuild

1. **Kiểm tra test endpoint:**
   - Mở browser: `http://localhost:5000/api/work-items/test`
   - Nếu trả về JSON, controller đang hoạt động

2. **Kiểm tra Swagger:**
   - Mở: `http://localhost:5000/swagger`
   - Test endpoint PUT `/api/work-items/{id}`

3. **Xem logs:**
   - Logs sẽ hiển thị trong console khi chạy `rebuild-and-run.ps1`
   - Logs sẽ cho biết:
     - Request có đến controller không
     - Có bao nhiêu work items trong database
     - Work item ID có tồn tại không

## Troubleshooting

### Lỗi 404 Not Found
1. Đảm bảo backend đã được rebuild và đang chạy
2. Kiểm tra logs để xem request có đến controller không
3. Kiểm tra database connection
4. Kiểm tra JWT token có hợp lệ không

### Build thất bại
1. Kiểm tra .NET SDK đã cài đặt: `dotnet --version`
2. Kiểm tra connection string trong `appsettings.json`
3. Xem chi tiết lỗi trong console

## Các endpoint test

- `GET /api/work-items/test` - Test controller hoạt động
- `GET /api/work-items` - Lấy tất cả work items
- `GET /api/work-items/{id}` - Lấy work item theo ID
- `PUT /api/work-items/{id}` - Cập nhật work item


