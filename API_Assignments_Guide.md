# 📘 Hướng Dẫn Sử Dụng API: Assignments

## Base Endpoint: `/api/Assignments`

API này quản lý các **Assignments** (Giao việc) trong hệ thống, bao gồm các thao tác CRUD cơ bản và các endpoint con để quản lý work items, work changes, và approvals.

---

## 🔐 Authentication

**Yêu cầu**: Tất cả request đều phải có JWT Token trong header.

```http
Authorization: Bearer {your_jwt_token}
```

---

## 📋 Danh Sách Endpoints

| Method | Endpoint | Mô Tả |
|--------|----------|-------|
| `GET` | `/api/Assignments` | Lấy danh sách tất cả assignments |
| `GET` | `/api/Assignments/{id}` | Lấy chi tiết một assignment |
| `POST` | `/api/Assignments` | Tạo assignment mới |
| `PUT` | `/api/Assignments/{id}` | Cập nhật assignment |
| `DELETE` | `/api/Assignments/{id}` | Xóa assignment |
| `POST` | `/api/Assignments/{id}/work-items` | Thêm work item vào assignment |
| `POST` | `/api/Assignments/{id}/work-changes` | Thêm work change vào assignment |
| `POST` | `/api/Assignments/{id}/approvals` | Thêm approval vào assignment |

---

## 1️⃣ GET /api/Assignments - Lấy Danh Sách Assignments

Lấy danh sách tất cả assignments, được sắp xếp theo ID giảm dần (mới nhất trước).

### Request

```http
GET /api/Assignments
Authorization: Bearer {token}
```

### Response (200 OK)

```json
[
  {
    "assignmentID": 2,
    "tbkt_ID": "TC: 97/QĐ-HDTV",
    "machineName": "MBA 3 pha 500kVA",
    "standardRequirement": "Io ≤ 2.0%",
    "additionalRequest": null,
    "deliveryDate": "2025-12-01T17:00:00.000Z",
    "designer": "2",
    "teamLeader": "4",
    "assignmentApprovals": [
      {
        "approvalID": 1,
        "assignmentID": 2,
        "approverRole": "Manager",
        "approverName": "Nguyễn Văn A",
        "approvalDate": "2025-11-10T10:00:00.000Z",
        "notes": "Approved"
      }
    ],
    "workChanges": [
      {
        "changeID": 1,
        "assignmentID": 2,
        "changeType": "Change Request",
        "description": "Thay đổi yêu cầu kỹ thuật"
      }
    ],
    "workItems": [
      {
        "workItemID": 1,
        "assignmentID": 2,
        "workType": "Core Design",
        "personName": "Ngô Cẩm Tú",
        "startDate": "2025-11-15T08:00:00.000Z",
        "expectedFinish": "2025-11-20T17:00:00.000Z",
        "actualFinish": null,
        "personConfirmation": false,
        "notes": null
      }
    ]
  },
  {
    "assignmentID": 1,
    "tbkt_ID": "TC: 96/QĐ-HDTV",
    "machineName": "MBA 3 pha 250kVA",
    "standardRequirement": "Io ≤ 2.0%",
    "additionalRequest": null,
    "deliveryDate": "2025-11-29T17:00:00.000Z",
    "designer": "2",
    "teamLeader": "4",
    "assignmentApprovals": [],
    "workChanges": [],
    "workItems": []
  }
]
```

### Ví Dụ Code

#### JavaScript (Fetch API)

```javascript
async function getAssignments() {
  const token = localStorage.getItem('token');
  
  const response = await fetch('http://localhost:5000/api/Assignments', {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  if (!response.ok) {
    throw new Error('Failed to fetch assignments');
  }

  return await response.json();
}

// Sử dụng
const assignments = await getAssignments();
console.log(assignments);
```

#### TypeScript/Angular

```typescript
getAssignments(): Observable<MachineAssignmentDto[]> {
  return this.http.get<MachineAssignmentDto[]>(
    `${this.apiUrl}/Assignments`,
    { headers: this.getHeaders() }
  );
}
```

---

## 2️⃣ GET /api/Assignments/{id} - Lấy Chi Tiết Assignment

Lấy thông tin chi tiết của một assignment cụ thể, bao gồm tất cả work items, work changes, và approvals.

### Request

```http
GET /api/Assignments/{id}
Authorization: Bearer {token}
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | `integer` | ✅ Yes | ID của Assignment cần lấy |

### Response (200 OK)

```json
{
  "assignmentID": 1,
  "tbkt_ID": "TC: 96/QĐ-HDTV",
  "machineName": "MBA 3 pha 250kVA, 35±2x2.5%/0.4kV Dyn11 (unicore 5 tru)",
  "standardRequirement": "Io ≤ 2.0% (+30%); Po ≤ 340 W; Pk ≤ 2600 W; Uk ≥ 4.0%\nDầu cách điện POWEROIL TO 1020 60 HX",
  "additionalRequest": "TC: 96/QĐ-HDTV",
  "deliveryDate": "2025-11-29T17:00:00.000Z",
  "designer": "2",
  "teamLeader": "4",
  "assignmentApprovals": [],
  "workChanges": [],
  "workItems": []
}
```

### Error Response (404 Not Found)

```json
{
  "error": "Assignment not found"
}
```

### Ví Dụ Code

```javascript
async function getAssignment(id) {
  const token = localStorage.getItem('token');
  
  const response = await fetch(`http://localhost:5000/api/Assignments/${id}`, {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  if (response.status === 404) {
    throw new Error('Assignment not found');
  }

  if (!response.ok) {
    throw new Error('Failed to fetch assignment');
  }

  return await response.json();
}
```

---

## 3️⃣ POST /api/Assignments - Tạo Assignment Mới

Tạo một assignment mới trong hệ thống.

### Request

```http
POST /api/Assignments
Content-Type: application/json
Authorization: Bearer {token}
```

### Request Body

```json
{
  "tbkt_ID": "TC: 96/QĐ-HDTV",
  "machineName": "MBA 3 pha 250kVA, 35±2x2.5%/0.4kV Dyn11 (unicore 5 tru)",
  "standardRequirement": "Io ≤ 2.0% (+30%); Po ≤ 340 W; Pk ≤ 2600 W; Uk ≥ 4.0%\nDầu cách điện POWEROIL TO 1020 60 HX",
  "additionalRequest": "TC: 96/QĐ-HDTV",
  "deliveryDate": "2025-11-29T17:00:00.000Z",
  "designer": "2",
  "teamLeader": "4"
}
```

### Các Trường Dữ Liệu

| Field | Type | Required | Max Length | Description |
|-------|------|----------|------------|-------------|
| `tbkt_ID` | `string` | ✅ Yes | 50 | Mã TBKT (Technical Sheet ID) |
| `machineName` | `string` | ✅ Yes | 255 | Tên máy/thiết bị |
| `standardRequirement` | `string` | ❌ No | 1000 | Yêu cầu tiêu chuẩn |
| `additionalRequest` | `string` | ❌ No | 1000 | Yêu cầu bổ sung |
| `deliveryDate` | `DateTime?` | ❌ No | - | Ngày giao hàng (ISO 8601) |
| `designer` | `string` | ❌ No | 100 | Người thiết kế |
| `teamLeader` | `string` | ❌ No | 100 | Trưởng nhóm |

### Response (201 Created)

```json
{
  "assignmentID": 1,
  "tbkt_ID": "TC: 96/QĐ-HDTV",
  "machineName": "MBA 3 pha 250kVA, 35±2x2.5%/0.4kV Dyn11 (unicore 5 tru)",
  "standardRequirement": "Io ≤ 2.0% (+30%); Po ≤ 340 W; Pk ≤ 2600 W; Uk ≥ 4.0%\nDầu cách điện POWEROIL TO 1020 60 HX",
  "additionalRequest": "TC: 96/QĐ-HDTV",
  "deliveryDate": "2025-11-29T17:00:00.000Z",
  "designer": "2",
  "teamLeader": "4",
  "assignmentApprovals": [],
  "workChanges": [],
  "workItems": []
}
```

### Error Responses

#### 400 Bad Request - Dữ liệu không hợp lệ

```json
{
  "tbkt_ID": ["The TBKT_ID field is required."],
  "machineName": ["The MachineName field is required."]
}
```

### Ví Dụ Code

#### cURL

```bash
curl -X POST "http://localhost:5000/api/Assignments" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "tbkt_ID": "TC: 96/QĐ-HDTV",
    "machineName": "MBA 3 pha 250kVA",
    "standardRequirement": "Io ≤ 2.0%",
    "deliveryDate": "2025-11-29T17:00:00.000Z",
    "designer": "2",
    "teamLeader": "4"
  }'
```

#### JavaScript

```javascript
async function createAssignment(assignmentData) {
  const token = localStorage.getItem('token');
  
  const response = await fetch('http://localhost:5000/api/Assignments', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(assignmentData)
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Failed to create assignment');
  }

  return await response.json();
}

// Sử dụng
const newAssignment = await createAssignment({
  tbkt_ID: "TC: 96/QĐ-HDTV",
  machineName: "MBA 3 pha 250kVA",
  standardRequirement: "Io ≤ 2.0%",
  deliveryDate: "2025-11-29T17:00:00.000Z",
  designer: "2",
  teamLeader: "4"
});
```

#### TypeScript/Angular

```typescript
createAssignment(data: CreateMachineAssignmentDto): Observable<MachineAssignmentDto> {
  return this.http.post<MachineAssignmentDto>(
    `${this.apiUrl}/Assignments`,
    data,
    { headers: this.getHeaders() }
  );
}
```

---

## 4️⃣ PUT /api/Assignments/{id} - Cập Nhật Assignment

Cập nhật thông tin của một assignment đã tồn tại. Tất cả các trường đều optional - chỉ cập nhật các trường được gửi lên.

### Request

```http
PUT /api/Assignments/{id}
Content-Type: application/json
Authorization: Bearer {token}
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | `integer` | ✅ Yes | ID của Assignment cần cập nhật |

### Request Body

```json
{
  "machineName": "MBA 3 pha 300kVA (Updated)",
  "deliveryDate": "2025-12-05T17:00:00.000Z",
  "designer": "3"
}
```

**Lưu ý**: Bạn chỉ cần gửi các trường muốn cập nhật. Các trường không gửi sẽ giữ nguyên giá trị cũ.

### Các Trường Có Thể Cập Nhật

| Field | Type | Max Length | Description |
|-------|------|------------|-------------|
| `tbkt_ID` | `string?` | 50 | Mã TBKT |
| `machineName` | `string?` | 255 | Tên máy/thiết bị |
| `standardRequirement` | `string?` | 1000 | Yêu cầu tiêu chuẩn |
| `additionalRequest` | `string?` | 1000 | Yêu cầu bổ sung |
| `deliveryDate` | `DateTime?` | - | Ngày giao hàng |
| `designer` | `string?` | 100 | Người thiết kế |
| `teamLeader` | `string?` | 100 | Trưởng nhóm |

### Response (204 No Content)

Không có response body khi thành công.

### Error Responses

#### 404 Not Found

```json
{
  "error": "Assignment not found"
}
```

### Ví Dụ Code

```javascript
async function updateAssignment(id, updateData) {
  const token = localStorage.getItem('token');
  
  const response = await fetch(`http://localhost:5000/api/Assignments/${id}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(updateData)
  });

  if (response.status === 404) {
    throw new Error('Assignment not found');
  }

  if (!response.ok) {
    throw new Error('Failed to update assignment');
  }

  // 204 No Content - không có response body
  return true;
}

// Sử dụng - chỉ cập nhật một số trường
await updateAssignment(1, {
  machineName: "MBA 3 pha 300kVA (Updated)",
  deliveryDate: "2025-12-05T17:00:00.000Z"
});
```

---

## 5️⃣ DELETE /api/Assignments/{id} - Xóa Assignment

Xóa một assignment khỏi hệ thống. **Cảnh báo**: Hành động này có thể xóa cả các work items, work changes, và approvals liên quan (tùy vào cấu hình database).

### Request

```http
DELETE /api/Assignments/{id}
Authorization: Bearer {token}
```

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | `integer` | ✅ Yes | ID của Assignment cần xóa |

### Response (204 No Content)

Không có response body khi thành công.

### Error Responses

#### 404 Not Found

```json
{
  "error": "Assignment not found"
}
```

### Ví Dụ Code

```javascript
async function deleteAssignment(id) {
  const token = localStorage.getItem('token');
  
  const response = await fetch(`http://localhost:5000/api/Assignments/${id}`, {
    method: 'DELETE',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  if (response.status === 404) {
    throw new Error('Assignment not found');
  }

  if (!response.ok) {
    throw new Error('Failed to delete assignment');
  }

  return true;
}

// Sử dụng
await deleteAssignment(1);
```

---

## 📝 Các Endpoint Con (Sub-endpoints)

### POST /api/Assignments/{id}/work-items

Thêm work item vào assignment. Xem chi tiết tại [API_WorkItems_Guide.md](./API_WorkItems_Guide.md)

### POST /api/Assignments/{id}/work-changes

Thêm work change vào assignment.

**Request Body**:
```json
{
  "assignmentID": 1,
  "changeType": "Change Request",
  "description": "Thay đổi yêu cầu kỹ thuật"
}
```

### POST /api/Assignments/{id}/approvals

Thêm approval vào assignment.

**Request Body**:
```json
{
  "assignmentID": 1,
  "approverRole": "Manager",
  "approverName": "Nguyễn Văn A",
  "approvalDate": "2025-11-10T10:00:00.000Z",
  "notes": "Approved"
}
```

---

## 💻 Ví Dụ Service Hoàn Chỉnh (TypeScript/Angular)

```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AssignmentService {
  private apiUrl = 'http://localhost:5000/api';

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  // Lấy danh sách assignments
  getAssignments(): Observable<MachineAssignmentDto[]> {
    return this.http.get<MachineAssignmentDto[]>(
      `${this.apiUrl}/Assignments`,
      { headers: this.getHeaders() }
    );
  }

  // Lấy chi tiết assignment
  getAssignment(id: number): Observable<MachineAssignmentDto> {
    return this.http.get<MachineAssignmentDto>(
      `${this.apiUrl}/Assignments/${id}`,
      { headers: this.getHeaders() }
    );
  }

  // Tạo assignment mới
  createAssignment(data: CreateMachineAssignmentDto): Observable<MachineAssignmentDto> {
    return this.http.post<MachineAssignmentDto>(
      `${this.apiUrl}/Assignments`,
      data,
      { headers: this.getHeaders() }
    );
  }

  // Cập nhật assignment
  updateAssignment(id: number, data: UpdateMachineAssignmentDto): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/Assignments/${id}`,
      data,
      { headers: this.getHeaders() }
    );
  }

  // Xóa assignment
  deleteAssignment(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/Assignments/${id}`,
      { headers: this.getHeaders() }
    );
  }
}

// Interface definitions
interface CreateMachineAssignmentDto {
  tbkt_ID: string;
  machineName: string;
  standardRequirement?: string;
  additionalRequest?: string;
  deliveryDate?: string;
  designer?: string;
  teamLeader?: string;
}

interface UpdateMachineAssignmentDto {
  tbkt_ID?: string;
  machineName?: string;
  standardRequirement?: string;
  additionalRequest?: string;
  deliveryDate?: string;
  designer?: string;
  teamLeader?: string;
}

interface MachineAssignmentDto {
  assignmentID: number;
  tbkt_ID: string;
  machineName: string;
  standardRequirement?: string;
  additionalRequest?: string;
  deliveryDate?: string;
  designer?: string;
  teamLeader?: string;
  assignmentApprovals?: AssignmentApprovalDto[];
  workChanges?: WorkChangeDto[];
  workItems?: WorkItemDto[];
}

interface AssignmentApprovalDto {
  approvalID: number;
  assignmentID: number;
  approverRole?: string;
  approverName?: string;
  approvalDate?: string;
  notes?: string;
}

interface WorkChangeDto {
  changeID: number;
  assignmentID: number;
  changeType?: string;
  description?: string;
}

interface WorkItemDto {
  workItemID: number;
  assignmentID: number;
  workType?: string;
  personName?: string;
  startDate?: string;
  expectedFinish?: string;
  actualFinish?: string;
  personConfirmation?: boolean;
  notes?: string;
}
```

---

## ⚠️ Lưu Ý Quan Trọng

1. **Format DateTime**: Sử dụng ISO 8601 format:
   - ✅ Đúng: `"2025-11-29T17:00:00.000Z"`
   - ❌ Sai: `"2025-11-29"` hoặc `"29/11/2025"`

2. **PUT Request**: Chỉ cập nhật các trường được gửi lên. Các trường không gửi sẽ giữ nguyên giá trị cũ.

3. **DELETE Request**: Hành động này có thể xóa cả dữ liệu liên quan. Cần cẩn thận khi sử dụng.

4. **Technical Sheet**: Khi tạo assignment mới, nếu `TBKT_ID` chưa tồn tại trong TechnicalSheets, hệ thống sẽ tự động tạo một TechnicalSheet mới.

5. **Sắp xếp**: GET danh sách assignments được sắp xếp theo `AssignmentID` giảm dần (mới nhất trước).

---

## 🔄 Workflow Điển Hình

1. **Tạo Assignment** → `POST /api/Assignments` → Nhận `assignmentID`
2. **Thêm Work Items** → `POST /api/Assignments/{id}/work-items` (nhiều lần)
3. **Thêm Work Changes** → `POST /api/Assignments/{id}/work-changes` (nếu cần)
4. **Thêm Approvals** → `POST /api/Assignments/{id}/approvals` (nếu cần)
5. **Xem Assignment** → `GET /api/Assignments/{id}` để xem tất cả thông tin
6. **Cập nhật Assignment** → `PUT /api/Assignments/{id}` khi cần thay đổi
7. **Xóa Assignment** → `DELETE /api/Assignments/{id}` (nếu cần)

---

## 📚 Tài Liệu Liên Quan

- [API Work Items Guide](./API_WorkItems_Guide.md) - Hướng dẫn chi tiết về Work Items
- [Frontend API Guide](./FRONTEND_API_GUIDE.md) - Hướng dẫn đầy đủ về Frontend API
- [API Documentation](./API_Documentation.md) - Tài liệu API tổng quan

---

## ❓ Câu Hỏi Thường Gặp (FAQ)

**Q: Làm sao để lấy danh sách assignments với filter/pagination?**  
A: Hiện tại API không hỗ trợ filter/pagination. Bạn cần lấy tất cả và filter ở phía client, hoặc yêu cầu backend team thêm tính năng này.

**Q: Có thể cập nhật một phần assignment không?**  
A: Có, sử dụng PUT với chỉ các trường cần cập nhật. Các trường không gửi sẽ giữ nguyên.

**Q: Xóa assignment có xóa cả work items không?**  
A: Tùy vào cấu hình database (cascade delete). Nên kiểm tra với backend team hoặc test trước.

**Q: Làm sao để tìm assignment theo TBKT_ID?**  
A: Hiện tại không có endpoint search. Bạn cần lấy tất cả assignments và filter ở client, hoặc yêu cầu backend team thêm endpoint search.

**Q: Response có bao gồm TechnicalSheet không?**  
A: Không, response chỉ bao gồm `TBKT_ID` (ID của TechnicalSheet), không bao gồm toàn bộ thông tin TechnicalSheet.

---

**Cập nhật lần cuối**: 2025-11-11

