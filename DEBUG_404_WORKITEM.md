# Debug 404 Error cho Work Items

## Vấn đề
PUT request đến `/api/work-items/66` trả về 404 Not Found mặc dù work item ID 66 tồn tại trong database.

## Các bước debug

### 1. Rebuild Backend
**QUAN TRỌNG:** Backend phải được rebuild sau mỗi lần sửa code!

```powershell
cd d:\Project\thibidi\quanlyfiles\quanlyfilesBE
.\quick-rebuild.ps1
```

Hoặc:
```powershell
dotnet build --no-incremental
```

### 2. Restart Backend
Nếu backend đang chạy, dừng (Ctrl+C) và chạy lại:
```powershell
dotnet run
```

Hoặc dùng script:
```powershell
.\rebuild-and-run.ps1
```

### 3. Kiểm tra Test Endpoint
Mở browser và test:
- `http://localhost:5000/api/work-items/test`
- Nếu trả về JSON, controller đang hoạt động
- Nếu 404, có vấn đề với routing hoặc backend chưa rebuild

### 4. Kiểm tra Logs
Khi gọi PUT `/api/work-items/66`, xem console logs của backend:

**Nếu thấy logs:**
```
[Routing Debug] PUT /api/work-items/66
UpdateWorkItem called with ID: 66, DTO: {...}
Total work items in database: X
All work item IDs in database: [66, 67, ...]
```
→ Request đã đến controller, kiểm tra database query

**Nếu KHÔNG thấy logs:**
→ Request không đến controller, có thể:
- Backend chưa được rebuild
- Routing không match
- Authorization chặn request

### 5. Kiểm tra Authorization
Nếu request bị chặn bởi authorization, sẽ thấy:
- Status 401 Unauthorized (không phải 404)
- Hoặc không có logs nào

**Để test không cần auth (tạm thời):**
Thêm `[AllowAnonymous]` vào method `UpdateWorkItem`:
```csharp
[HttpPut("{id}")]
[AllowAnonymous] // Tạm thời để test
public async Task<IActionResult> UpdateWorkItem(int id, [FromBody] UpdateWorkItemDto dto)
```

### 6. Kiểm tra Database
Kiểm tra work item ID 66 có tồn tại:
```sql
SELECT * FROM WorkItem WHERE WorkItemID = 66
```

### 7. Kiểm tra Route
Route template: `[Route("api/[controller]")]` → `api/work-items`
Method: `[HttpPut("{id}")]` → `PUT /api/work-items/{id}`

Đảm bảo frontend gọi đúng:
- URL: `http://localhost:5000/api/work-items/66`
- Method: `PUT`
- Headers: `Content-Type: application/json`, `Authorization: Bearer ...`

## Các thay đổi đã thực hiện

1. ✅ Thay `FindAsync` bằng `FirstOrDefaultAsync` để query trực tiếp từ database
2. ✅ Thêm logging chi tiết trong controller
3. ✅ Thêm logging middleware để debug routing
4. ✅ Thêm test endpoint `/api/work-items/test`
5. ✅ Cấu hình JSON serializer để nhận camelCase từ frontend
6. ✅ Sửa frontend để gửi workType bằng tiếng Anh

## Checklist

- [ ] Backend đã được rebuild
- [ ] Backend đang chạy (port 5000)
- [ ] Test endpoint `/api/work-items/test` hoạt động
- [ ] Logs hiển thị khi gọi PUT request
- [ ] Database có work item ID 66
- [ ] JWT token hợp lệ
- [ ] Frontend gửi đúng URL và method

## Nếu vẫn lỗi 404

1. Kiểm tra Swagger: `http://localhost:5000/swagger`
   - Xem endpoint PUT `/api/work-items/{id}` có hiển thị không
   - Test trực tiếp từ Swagger

2. Kiểm tra Network tab trong browser:
   - Request URL có đúng không
   - Status code là gì (404 hay 401?)
   - Response body có gì

3. Kiểm tra backend logs:
   - Có thấy request đến không
   - Có error nào không

4. Thử tạm thời bỏ `[Authorize]` ở controller level để test


