# Quản Lý Files Backend API

API Backend .NET Core phục vụ cho ứng dụng quản lý files (Angular Frontend)

## 📋 Tính năng

- ✅ CORS được cấu hình cho Angular frontend (http://localhost:4200)
- ✅ Swagger UI để test API
- ✅ RESTful API endpoints cho quản lý files và folders
- ✅ Data Transfer Objects (DTOs) với validation
- ✅ **Entity Framework Core với SQL Server**
- ✅ **Database Migrations**
- ✅ API tiếng Việt

## 🛠️ Cài đặt và chạy

### Yêu cầu
- .NET 9.0 SDK hoặc cao hơn
- SQL Server (hoặc SQL Server Express)
- Entity Framework Core Tools

### Setup Database

1. **Cấu hình Connection String** trong `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=quanlyphancong;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

2. **Tạo và cập nhật Database**:
```bash
cd quanlyfilesBE

# Tạo migration (nếu chưa có)
dotnet ef migrations add InitialCreate

# Cập nhật database
dotnet ef database update
```

### Chạy ứng dụng
```bash
cd quanlyfilesBE
dotnet run
```

API sẽ chạy tại: `https://localhost:5001` hoặc `http://localhost:5000`

### User test chức năng email

Để test gửi/nhận email (Power Automate, thông báo): tạo user test **tungchinhus@gmail.com** bằng script:

```powershell
.\Scripts\create-test-email-user.ps1
```

Chi tiết: [TEST_EMAIL_USER.md](TEST_EMAIL_USER.md)

## 📡 API Endpoints

### Files API

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `api/Files` | Lấy danh sách tất cả files |
| GET | `api/Files/{id}` | Lấy thông tin file theo ID |
| GET | `api/Files/byType/{fileType}` | Lấy files theo loại |
| POST | `api/Files` | Tạo file mới |
| PUT | `api/Files/{id}` | Cập nhật file |
| DELETE | `api/Files/{id}` | Xóa file |

### Folders API

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `api/Folders` | Lấy danh sách tất cả folders |
| GET | `api/Folders/{id}` | Lấy thông tin folder theo ID |
| GET | `api/Folders/root` | Lấy danh sách root folders |
| POST | `api/Folders` | Tạo folder mới |
| PUT | `api/Folders/{id}` | Cập nhật folder |
| DELETE | `api/Folders/{id}` | Xóa folder |

## 📚 Swagger Documentation

Khi chạy ở môi trường Development, truy cập Swagger UI tại:
`https://localhost:5001/swagger`

## 🌐 CORS Configuration

API đã được cấu hình CORS để cho phép Angular frontend chạy tại `http://localhost:4200`

## 💻 Sử dụng trong Angular

Trong Angular service, bạn có thể gọi API như sau:

```typescript
// File Service Example
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FileService {
  private apiUrl = 'https://localhost:5001/api/Files';

  constructor(private http: HttpClient) { }

  getFiles(): Observable<FileItem[]> {
    return this.http.get<FileItem[]>(this.apiUrl);
  }

  getFile(id: number): Observable<FileItem> {
    return this.http.get<FileItem>(`${this.apiUrl}/${id}`);
  }

  createFile(file: CreateFileItemDto): Observable<FileItem> {
    return this.http.post<FileItem>(this.apiUrl, file);
  }

  updateFile(id: number, file: UpdateFileItemDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, file);
  }

  deleteFile(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
```

## 📦 Models

### FileItem
```json
{
  "id": int,
  "fileName": "string",
  "filePath": "string",
  "fileType": "string",
  "fileSize": long,
  "uploadDate": "datetime",
  "uploadedBy": "string",
  "description": "string"
}
```

### Folder
```json
{
  "id": int,
  "folderName": "string",
  "folderPath": "string",
  "parentFolderId": int (nullable),
  "createdDate": "datetime",
  "createdBy": "string"
}
```

## 🏗️ Cấu trúc Project

```
quanlyfilesBE/
├── Controllers/
│   ├── FilesController.cs      # API quản lý files (với EF Core)
│   └── FoldersController.cs    # API quản lý folders (với EF Core)
├── Data/
│   └── ApplicationDbContext.cs # DbContext với EF Core
├── Models/
│   ├── FileItem.cs             # Model File
│   └── Folder.cs               # Model Folder
├── DTOs/
│   └── FileItemDto.cs          # Data Transfer Objects
├── Migrations/                  # EF Core Migrations
├── Properties/
│   └── launchSettings.json     # Cấu hình launch
├── Program.cs                   # Entry point với CORS, Swagger và DbContext
└── appsettings.json            # Connection string và cấu hình
```

## 🗄️ Database Schema

### Tables

#### Files
- `Id` (int, Primary Key, Identity)
- `FileName` (nvarchar(255), Required)
- `FilePath` (nvarchar(500), Required)
- `FileType` (nvarchar(100))
- `FileSize` (bigint)
- `UploadDate` (datetime2)
- `UploadedBy` (nvarchar(100))
- `Description` (nvarchar(500), Nullable)
- Indexes: `FileName`, `FileType`

#### Folders
- `Id` (int, Primary Key, Identity)
- `FolderName` (nvarchar(255), Required)
- `FolderPath` (nvarchar(500), Required)
- `ParentFolderId` (int, Nullable, Foreign Key → Folders)
- `CreatedDate` (datetime2)
- `CreatedBy` (nvarchar(100))
- Indexes: `FolderName`, `ParentFolderId`

## 🔧 Entity Framework Commands

```bash
# Tạo migration mới
dotnet ef migrations add MigrationName

# Cập nhật database
dotnet ef database update

# Xóa migration cuối cùng (chưa apply)
dotnet ef migrations remove

# Xem danh sách migrations
dotnet ef migrations list

# Rollback về migration trước đó
dotnet ef database update PreviousMigrationName
```

