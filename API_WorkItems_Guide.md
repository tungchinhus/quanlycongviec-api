# 📘 Hướng Dẫn Sử Dụng API: Work Items

## Endpoint: `POST /api/Assignments/{id}/work-items`

API này dùng để thêm một **Work Item** (công việc con) vào một **Assignment** (giao việc) đã tồn tại.

---

## 🔐 Authentication

**Yêu cầu**: Tất cả request đều phải có JWT Token trong header.

```http
Authorization: Bearer {your_jwt_token}
```

---

## 📋 Thông Tin Endpoint

- **Method**: `POST`
- **URL**: `/api/Assignments/{id}/work-items`
- **Content-Type**: `application/json`
- **Authentication**: Required (Bearer Token)

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | `integer` | ✅ Yes | ID của Assignment cần thêm work item |

---

## 📤 Request Body

### CreateWorkItemDto

```json
{
  "assignmentID": 1,
  "workType": "Core Design",
  "personName": "Ngô Cẩm Tú",
  "startDate": "2025-11-15T08:00:00.000Z",
  "expectedFinish": "2025-11-20T17:00:00.000Z",
  "actualFinish": null,
  "personConfirmation": false,
  "notes": "Ghi chú về công việc"
}
```

### Các Trường Dữ Liệu

| Field | Type | Required | Max Length | Description |
|-------|------|----------|------------|-------------|
| `assignmentID` | `integer` | ✅ Yes | - | ID của Assignment (phải khớp với `{id}` trong URL) |
| `workType` | `string` | ❌ No | 100 | Loại công việc (ví dụ: "Core Design", "Core Review", "Casing Design", etc.) |
| `personName` | `string` | ❌ No | 100 | Tên người thực hiện công việc |
| `startDate` | `DateTime?` | ❌ No | - | Ngày bắt đầu (ISO 8601 format) |
| `expectedFinish` | `DateTime?` | ❌ No | - | Ngày dự kiến hoàn thành (ISO 8601 format) |
| `actualFinish` | `DateTime?` | ❌ No | - | Ngày thực tế hoàn thành (ISO 8601 format) |
| `personConfirmation` | `boolean?` | ❌ No | - | Xác nhận của người thực hiện (true/false/null) |
| `notes` | `string` | ❌ No | 500 | Ghi chú về công việc |

---

## 📥 Response

### Success Response (201 Created)

```json
{
  "workItemID": 1,
  "assignmentID": 1,
  "workType": "Core Design",
  "personName": "Ngô Cẩm Tú",
  "startDate": "2025-11-15T08:00:00.000Z",
  "expectedFinish": "2025-11-20T17:00:00.000Z",
  "actualFinish": null,
  "personConfirmation": false,
  "notes": "Ghi chú về công việc"
}
```

### Error Responses

#### 404 Not Found - Assignment không tồn tại

```json
{
  "error": "Assignment not found"
}
```

#### 400 Bad Request - Dữ liệu không hợp lệ

```json
{
  "assignmentID": ["The AssignmentID field is required."]
}
```

#### 401 Unauthorized - Thiếu hoặc Token không hợp lệ

```json
{
  "error": "Unauthorized"
}
```

#### 500 Internal Server Error

```json
{
  "error": "Error adding work item",
  "message": "Chi tiết lỗi..."
}
```

---

## 💡 Ví Dụ Sử Dụng

### 1. Ví Dụ với cURL

```bash
curl -X POST "http://localhost:5000/api/Assignments/1/work-items" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "assignmentID": 1,
    "workType": "Core Design",
    "personName": "Ngô Cẩm Tú",
    "startDate": "2025-11-15T08:00:00.000Z",
    "expectedFinish": "2025-11-20T17:00:00.000Z",
    "personConfirmation": false,
    "notes": "Thiết kế ruột máy biến áp"
  }'
```

### 2. Ví Dụ với JavaScript (Fetch API)

```javascript
async function createWorkItem(assignmentId, workItemData) {
  const token = localStorage.getItem('token'); // Lấy token từ storage
  
  const response = await fetch(
    `http://localhost:5000/api/Assignments/${assignmentId}/work-items`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({
        assignmentID: assignmentId,
        workType: workItemData.workType,
        personName: workItemData.personName,
        startDate: workItemData.startDate,
        expectedFinish: workItemData.expectedFinish,
        actualFinish: workItemData.actualFinish,
        personConfirmation: workItemData.personConfirmation,
        notes: workItemData.notes
      })
    }
  );

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Failed to create work item');
  }

  return await response.json();
}

// Sử dụng
const workItem = await createWorkItem(1, {
  workType: "Core Design",
  personName: "Ngô Cẩm Tú",
  startDate: "2025-11-15T08:00:00.000Z",
  expectedFinish: "2025-11-20T17:00:00.000Z",
  personConfirmation: false,
  notes: "Thiết kế ruột máy biến áp"
});
```

### 3. Ví Dụ với TypeScript/Angular

```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class WorkItemService {
  private apiUrl = 'http://localhost:5000/api/Assignments';

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  createWorkItem(assignmentId: number, workItem: CreateWorkItemDto): Observable<WorkItemDto> {
    return this.http.post<WorkItemDto>(
      `${this.apiUrl}/${assignmentId}/work-items`,
      workItem,
      { headers: this.getHeaders() }
    );
  }
}

// Interface definitions
interface CreateWorkItemDto {
  assignmentID: number;
  workType?: string;
  personName?: string;
  startDate?: string | null;
  expectedFinish?: string | null;
  actualFinish?: string | null;
  personConfirmation?: boolean | null;
  notes?: string | null;
}

interface WorkItemDto {
  workItemID: number;
  assignmentID: number;
  workType?: string;
  personName?: string;
  startDate?: string | null;
  expectedFinish?: string | null;
  actualFinish?: string | null;
  personConfirmation?: boolean | null;
  notes?: string | null;
}

// Sử dụng trong Component
export class AssignmentComponent {
  constructor(private workItemService: WorkItemService) {}

  addWorkItem(assignmentId: number) {
    const workItem: CreateWorkItemDto = {
      assignmentID: assignmentId,
      workType: "Core Design",
      personName: "Ngô Cẩm Tú",
      startDate: "2025-11-15T08:00:00.000Z",
      expectedFinish: "2025-11-20T17:00:00.000Z",
      personConfirmation: false,
      notes: "Thiết kế ruột máy biến áp"
    };

    this.workItemService.createWorkItem(assignmentId, workItem).subscribe({
      next: (result) => {
        console.log('Work item created:', result);
      },
      error: (error) => {
        console.error('Error creating work item:', error);
      }
    });
  }
}
```

### 4. Ví Dụ với Axios (React/Vue)

```javascript
import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json'
  }
});

// Thêm interceptor để tự động thêm token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Hàm tạo work item
async function createWorkItem(assignmentId, workItemData) {
  try {
    const response = await api.post(
      `/Assignments/${assignmentId}/work-items`,
      {
        assignmentID: assignmentId,
        ...workItemData
      }
    );
    return response.data;
  } catch (error) {
    console.error('Error creating work item:', error.response?.data || error.message);
    throw error;
  }
}

// Sử dụng
const workItem = await createWorkItem(1, {
  workType: "Core Design",
  personName: "Ngô Cẩm Tú",
  startDate: "2025-11-15T08:00:00.000Z",
  expectedFinish: "2025-11-20T17:00:00.000Z",
  notes: "Thiết kế ruột máy biến áp"
});
```

---

## 📝 Các Loại Work Type Phổ Biến

Dựa trên hệ thống, các loại công việc thường được sử dụng:

1. **Core Design** - Thiết kế ruột
2. **Core Review** - Kiểm soát ruột
3. **Casing Design** - Thiết kế vỏ
4. **Casing Review** - Kiểm soát vỏ
5. **Material Leveling** - Định mức vật tư

---

## ⚠️ Lưu Ý Quan Trọng

1. **AssignmentID phải tồn tại**: Trước khi tạo work item, đảm bảo Assignment với ID tương ứng đã được tạo.

2. **AssignmentID trong body phải khớp với URL**: 
   - URL: `/api/Assignments/1/work-items`
   - Body: `{ "assignmentID": 1, ... }`

3. **Format DateTime**: Sử dụng ISO 8601 format:
   - ✅ Đúng: `"2025-11-15T08:00:00.000Z"`
   - ❌ Sai: `"2025-11-15"` hoặc `"15/11/2025"`

4. **Tất cả các trường đều optional** (trừ `assignmentID`), bạn có thể gửi `null` hoặc bỏ qua các trường không cần thiết.

5. **Xác thực Token**: Đảm bảo token còn hiệu lực và có quyền truy cập.

---

## 🔄 Workflow Điển Hình

1. **Tạo Assignment** → Nhận `assignmentID`
2. **Tạo Work Items** → Gọi API này nhiều lần với các `workType` khác nhau
3. **Cập nhật Work Items** → (Nếu có endpoint PUT/PATCH)
4. **Xem Assignment** → GET `/api/Assignments/{id}` để xem tất cả work items

---

## 📚 Tài Liệu Liên Quan

- [API Assignments Guide](./FRONTEND_API_GUIDE.md) - Hướng dẫn đầy đủ về Assignments API
- [API Documentation](./API_Documentation.md) - Tài liệu API tổng quan
- [Frontend API Examples](./FRONTEND_API_EXAMPLES.js) - Ví dụ code frontend

---

## ❓ Câu Hỏi Thường Gặp (FAQ)

**Q: Có thể tạo nhiều work items cùng lúc không?**  
A: Không, API này chỉ tạo một work item mỗi lần gọi. Bạn cần gọi API nhiều lần cho từng work item.

**Q: Work item có thể cập nhật sau khi tạo không?**  
A: Hiện tại endpoint này chỉ hỗ trợ tạo mới. Để cập nhật, bạn cần kiểm tra xem có endpoint PUT/PATCH cho work items không.

**Q: Làm sao để xem danh sách work items của một assignment?**  
A: Gọi GET `/api/Assignments/{id}` - response sẽ bao gồm mảng `workItems`.

**Q: Có giới hạn số lượng work items cho một assignment không?**  
A: Không có giới hạn cứng trong code, nhưng nên kiểm tra với backend team về best practices.

---

**Cập nhật lần cuối**: 2025-11-11

