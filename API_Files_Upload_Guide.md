# 📁 Hướng Dẫn Frontend - API Upload File

**Base URL**: `http://localhost:5000/api` hoặc `https://localhost:5001/api`

---

## 🔐 Authentication

Tất cả API đều yêu cầu JWT Token trong header:

```http
Authorization: Bearer {your_jwt_token}
```

---

## 📤 API Upload File

### 1. Upload File cho Assignment

**Endpoint**: `POST /api/files/upload`

**Headers**:
```http
Authorization: Bearer {token}
```

**Lưu ý**: Không cần set `Content-Type` header, browser sẽ tự động set `multipart/form-data` với boundary.

**Request Body** (FormData):
- `file` (File, **bắt buộc**): File cần upload
- `assignmentId` (number, **bắt buộc**): ID của assignment
- `description` (string, optional): Mô tả file

**Giới hạn**:
- File size tối đa: **50MB**
- File sẽ được lưu với tên unique (GUID) để tránh trùng lặp

**Response (200 OK)**:
```json
{
  "id": 1,
  "fileName": "document.pdf",
  "filePath": "C:\\Files\\a1b2c3d4-e5f6-7890-abcd-ef1234567890.pdf",
  "fileType": "application/pdf",
  "fileSize": 291789,
  "uploadDate": "2025-11-12T10:30:00",
  "uploadedBy": "username",
  "description": "Mô tả file",
  "assignmentId": 123
}
```

**Lưu ý quan trọng**:
- `assignmentId` là **bắt buộc** và phải > 0
- File sẽ tự động được liên kết với assignment thông qua `AssignmentID`
- `MachineAssignment.FilePath` sẽ tự động được cập nhật với filePath mới (nối bằng dấu `;` nếu có nhiều file)

---

## 📥 API Lấy Files

### 1. Lấy tất cả Files

**Endpoint**: `GET /api/files`

**Headers**:
```http
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
[
  {
    "id": 1,
    "assignmentID": 123,
    "fileName": "document.pdf",
    "filePath": "C:\\Files\\a1b2c3d4-e5f6-7890-abcd-ef1234567890.pdf",
    "fileType": "application/pdf",
    "fileSize": 291789,
    "uploadDate": "2025-11-12T10:30:00",
    "uploadedBy": "username",
    "description": "Mô tả file"
  }
]
```

---

### 2. Lấy File theo ID

**Endpoint**: `GET /api/files/{id}`

**Headers**:
```http
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
  "id": 1,
  "assignmentID": 123,
  "fileName": "document.pdf",
  "filePath": "C:\\Files\\a1b2c3d4-e5f6-7890-abcd-ef1234567890.pdf",
  "fileType": "application/pdf",
  "fileSize": 291789,
  "uploadDate": "2025-11-12T10:30:00",
  "uploadedBy": "username",
  "description": "Mô tả file"
}
```

---

### 3. Lấy Files theo AssignmentID

**Endpoint**: `GET /api/files/byAssignment/{assignmentId}`

**Headers**:
```http
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
[
  {
    "id": 1,
    "assignmentID": 123,
    "fileName": "document1.pdf",
    "filePath": "C:\\Files\\file1.pdf",
    "fileType": "application/pdf",
    "fileSize": 291789,
    "uploadDate": "2025-11-12T10:30:00",
    "uploadedBy": "username",
    "description": "File 1"
  },
  {
    "id": 2,
    "assignmentID": 123,
    "fileName": "document2.pdf",
    "filePath": "C:\\Files\\file2.pdf",
    "fileType": "application/pdf",
    "fileSize": 150000,
    "uploadDate": "2025-11-12T11:00:00",
    "uploadedBy": "username",
    "description": "File 2"
  }
]
```

**Lưu ý**: Files được sắp xếp theo `uploadDate` giảm dần (mới nhất trước).

---

### 4. Lấy Files theo FileType

**Endpoint**: `GET /api/files/byType/{fileType}`

**Headers**:
```http
Authorization: Bearer {token}
```

**Ví dụ**: `GET /api/files/byType/pdf`

**Response (200 OK)**: Tương tự như lấy tất cả files, nhưng chỉ trả về files có `fileType` chứa "pdf".

---

## 🗑️ API Xóa File

### 1. Xóa File

**Endpoint**: `DELETE /api/files/{id}`

**Headers**:
```http
Authorization: Bearer {token}
```

**Response (204 No Content)**: Không có body response.

**Lưu ý quan trọng**:
- Khi xóa file, hệ thống sẽ tự động:
  1. Xóa file vật lý trên disk
  2. Xóa record trong database
  3. **Tự động cập nhật `MachineAssignment.FilePath`** để loại bỏ filePath đã xóa
  4. Nếu file là file cuối cùng, `MachineAssignment.FilePath` sẽ được set thành `null`

---

## 📝 Ví dụ Code Frontend

### JavaScript/TypeScript (Vanilla)

```javascript
// Upload file
async function uploadFile(file, assignmentId, description = null) {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('assignmentId', assignmentId.toString());
  if (description) {
    formData.append('description', description);
  }

  const token = localStorage.getItem('token');
  
  try {
    const response = await fetch('http://localhost:5000/api/files/upload', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`
        // KHÔNG set Content-Type, browser sẽ tự động set multipart/form-data
      },
      body: formData
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Upload failed');
    }

    const result = await response.json();
    console.log('File uploaded:', result);
    return result;
  } catch (error) {
    console.error('Upload error:', error);
    throw error;
  }
}

// Lấy files theo assignment
async function getFilesByAssignment(assignmentId) {
  const token = localStorage.getItem('token');
  
  try {
    const response = await fetch(
      `http://localhost:5000/api/files/byAssignment/${assignmentId}`,
      {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      }
    );

    if (!response.ok) {
      throw new Error('Failed to get files');
    }

    const files = await response.json();
    return files;
  } catch (error) {
    console.error('Get files error:', error);
    throw error;
  }
}

// Xóa file
async function deleteFile(fileId) {
  const token = localStorage.getItem('token');
  
  try {
    const response = await fetch(
      `http://localhost:5000/api/files/${fileId}`,
      {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      }
    );

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Delete failed');
    }

    console.log('File deleted successfully');
    return true;
  } catch (error) {
    console.error('Delete error:', error);
    throw error;
  }
}

// Sử dụng với HTML input
document.getElementById('fileInput').addEventListener('change', async (e) => {
  const file = e.target.files[0];
  if (!file) return;

  // Validate file size (50MB)
  if (file.size > 50 * 1024 * 1024) {
    alert('File size exceeds 50MB');
    return;
  }

  const assignmentId = 123; // Lấy từ form hoặc state
  const description = 'Mô tả file'; // Optional

  try {
    const result = await uploadFile(file, assignmentId, description);
    console.log('Upload successful:', result);
    alert('Upload thành công!');
  } catch (error) {
    alert('Upload thất bại: ' + error.message);
  }
});
```

---

### React Example

```jsx
import React, { useState } from 'react';

function FileUpload({ assignmentId }) {
  const [files, setFiles] = useState([]);
  const [uploading, setUploading] = useState(false);

  const handleFileUpload = async (event) => {
    const file = event.target.files[0];
    if (!file) return;

    // Validate file size
    if (file.size > 50 * 1024 * 1024) {
      alert('File size exceeds 50MB');
      return;
    }

    setUploading(true);
    const formData = new FormData();
    formData.append('file', file);
    formData.append('assignmentId', assignmentId.toString());
    formData.append('description', 'File description');

    const token = localStorage.getItem('token');

    try {
      const response = await fetch('http://localhost:5000/api/files/upload', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        },
        body: formData
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || 'Upload failed');
      }

      const result = await response.json();
      setFiles([...files, result]);
      alert('Upload thành công!');
    } catch (error) {
      alert('Upload thất bại: ' + error.message);
    } finally {
      setUploading(false);
    }
  };

  const handleDeleteFile = async (fileId) => {
    if (!confirm('Bạn có chắc muốn xóa file này?')) return;

    const token = localStorage.getItem('token');

    try {
      const response = await fetch(`http://localhost:5000/api/files/${fileId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (!response.ok) {
        throw new Error('Delete failed');
      }

      setFiles(files.filter(f => f.id !== fileId));
      alert('Xóa file thành công!');
    } catch (error) {
      alert('Xóa file thất bại: ' + error.message);
    }
  };

  // Load files on mount
  React.useEffect(() => {
    const loadFiles = async () => {
      const token = localStorage.getItem('token');
      try {
        const response = await fetch(
          `http://localhost:5000/api/files/byAssignment/${assignmentId}`,
          {
            headers: {
              'Authorization': `Bearer ${token}`
            }
          }
        );
        if (response.ok) {
          const data = await response.json();
          setFiles(data);
        }
      } catch (error) {
        console.error('Load files error:', error);
      }
    };

    if (assignmentId) {
      loadFiles();
    }
  }, [assignmentId]);

  return (
    <div>
      <input
        type="file"
        onChange={handleFileUpload}
        disabled={uploading}
      />
      {uploading && <p>Đang upload...</p>}

      <h3>Danh sách files:</h3>
      <ul>
        {files.map(file => (
          <li key={file.id}>
            {file.fileName} ({file.fileSize} bytes)
            <button onClick={() => handleDeleteFile(file.id)}>Xóa</button>
          </li>
        ))}
      </ul>
    </div>
  );
}

export default FileUpload;
```

---

### Angular Example

```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FileService {
  private apiUrl = 'http://localhost:5000/api';

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }

  uploadFile(file: File, assignmentId: number, description?: string): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('assignmentId', assignmentId.toString());
    if (description) {
      formData.append('description', description);
    }

    return this.http.post(`${this.apiUrl}/files/upload`, formData, {
      headers: this.getHeaders()
    });
  }

  getFilesByAssignment(assignmentId: number): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.apiUrl}/files/byAssignment/${assignmentId}`,
      { headers: this.getHeaders() }
    );
  }

  deleteFile(fileId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/files/${fileId}`,
      { headers: this.getHeaders() }
    );
  }
}
```

```typescript
// Component
import { Component, OnInit } from '@angular/core';
import { FileService } from './file.service';

@Component({
  selector: 'app-file-upload',
  template: `
    <input type="file" (change)="onFileSelected($event)" [disabled]="uploading">
    <p *ngIf="uploading">Đang upload...</p>
    
    <h3>Danh sách files:</h3>
    <ul>
      <li *ngFor="let file of files">
        {{ file.fileName }} ({{ file.fileSize }} bytes)
        <button (click)="deleteFile(file.id)">Xóa</button>
      </li>
    </ul>
  `
})
export class FileUploadComponent implements OnInit {
  files: any[] = [];
  uploading = false;
  assignmentId = 123; // Lấy từ route hoặc input

  constructor(private fileService: FileService) {}

  ngOnInit() {
    this.loadFiles();
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (!file) return;

    if (file.size > 50 * 1024 * 1024) {
      alert('File size exceeds 50MB');
      return;
    }

    this.uploading = true;
    this.fileService.uploadFile(file, this.assignmentId, 'Description')
      .subscribe({
        next: (result) => {
          this.files.push(result);
          alert('Upload thành công!');
          this.uploading = false;
        },
        error: (error) => {
          alert('Upload thất bại: ' + error.message);
          this.uploading = false;
        }
      });
  }

  loadFiles() {
    this.fileService.getFilesByAssignment(this.assignmentId)
      .subscribe({
        next: (files) => this.files = files,
        error: (error) => console.error('Load files error:', error)
      });
  }

  deleteFile(fileId: number) {
    if (!confirm('Bạn có chắc muốn xóa file này?')) return;

    this.fileService.deleteFile(fileId)
      .subscribe({
        next: () => {
          this.files = this.files.filter(f => f.id !== fileId);
          alert('Xóa file thành công!');
        },
        error: (error) => alert('Xóa file thất bại: ' + error.message)
      });
  }
}
```

---

## ⚠️ Lỗi thường gặp

### 1. 405 Method Not Allowed
- **Nguyên nhân**: Endpoint không đúng hoặc method không đúng
- **Giải pháp**: Kiểm tra URL là `/api/files/upload` và method là `POST`

### 2. 400 Bad Request - "assignmentId is required"
- **Nguyên nhân**: Không gửi `assignmentId` hoặc `assignmentId <= 0`
- **Giải pháp**: Đảm bảo gửi `assignmentId` trong FormData và giá trị > 0

### 3. 404 Not Found - "Assignment not found"
- **Nguyên nhân**: `assignmentId` không tồn tại trong database
- **Giải pháp**: Kiểm tra `assignmentId` có đúng không, tạo assignment trước khi upload file

### 4. 400 Bad Request - "File size exceeds maximum allowed size (50MB)"
- **Nguyên nhân**: File quá lớn
- **Giải pháp**: Giảm kích thước file hoặc nén file trước khi upload

### 5. 401 Unauthorized
- **Nguyên nhân**: Token không hợp lệ hoặc đã hết hạn
- **Giải pháp**: Đăng nhập lại để lấy token mới

---

## 📌 Lưu ý quan trọng

1. **assignmentId là bắt buộc**: Mỗi file phải được upload với `assignmentId` để xác định file thuộc assignment nào

2. **FilePath tự động cập nhật**: Khi upload file, `MachineAssignment.FilePath` sẽ tự động được cập nhật với filePath mới (nối bằng dấu `;` nếu có nhiều file)

3. **Xóa file tự động cập nhật**: Khi xóa file, `MachineAssignment.FilePath` sẽ tự động được cập nhật để loại bỏ filePath đã xóa

4. **File size limit**: Tối đa 50MB mỗi file

5. **File name unique**: File sẽ được lưu với tên unique (GUID) để tránh trùng lặp, nhưng `fileName` trong database vẫn giữ tên gốc

6. **Không set Content-Type**: Khi upload file với FormData, không nên set `Content-Type` header, browser sẽ tự động set `multipart/form-data` với boundary phù hợp

---

## 🔗 Liên kết

- [API Assignments Guide](./API_Assignments_Guide.md)
- [Frontend API Guide](./FRONTEND_API_GUIDE.md)
- [Frontend API Examples](./FRONTEND_API_EXAMPLES.js)

