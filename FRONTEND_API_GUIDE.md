# 📘 Hướng Dẫn Frontend Gọi API - QuanLyFiles Backend

**Base URL**: `http://localhost:5000/api` hoặc `https://localhost:5001/api`

---

## 🔐 Authentication

Tất cả API (trừ login/register) đều yêu cầu JWT Token trong header:

```http
Authorization: Bearer {your_jwt_token}
```

---

## 📋 API Tạo Giao Việc Mới (Assignments)

### 1. Tạo Assignment Mới

**Endpoint**: `POST /api/assignments`

**Headers**:
```http
Content-Type: application/json
Authorization: Bearer {token}
```

**Request Body**:
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

**Response (201 Created)**:
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

**Lưu ý**: 
- `tbkt_ID` và `machineName` là **bắt buộc** (required)
- `deliveryDate` phải là ISO 8601 format (YYYY-MM-DDTHH:mm:ss.sssZ)
- Sau khi tạo assignment, bạn sẽ nhận được `assignmentID` để tạo các work items

---

### 2. Tạo Work Items (Các công việc con)

Sau khi tạo assignment thành công, bạn cần tạo các work items riêng biệt cho từng loại công việc.

**Endpoint**: `POST /api/assignments/{assignmentID}/work-items`

**Headers**:
```http
Content-Type: application/json
Authorization: Bearer {token}
```

#### a) Tạo Work Item - Thiết kế ruột (Core Design)

**Request Body**:
```json
{
  "assignmentID": 1,
  "workType": "Core Design",
  "personName": "Ngô Cẩm Tú",
  "startDate": null,
  "expectedFinish": null,
  "actualFinish": null,
  "personConfirmation": null,
  "notes": null
}
```

#### b) Tạo Work Item - Kiểm soát ruột (Core Review)

**Request Body**:
```json
{
  "assignmentID": 1,
  "workType": "Core Review",
  "personName": "Trần Phước Ngọc Châu",
  "startDate": null,
  "expectedFinish": null,
  "actualFinish": null,
  "personConfirmation": null,
  "notes": null
}
```

#### c) Tạo Work Item - Thiết kế vỏ (Casing Design)

**Request Body**:
```json
{
  "assignmentID": 1,
  "workType": "Casing Design",
  "personName": "Phạm Hoàng Anh Tuấn",
  "startDate": null,
  "expectedFinish": null,
  "actualFinish": null,
  "personConfirmation": null,
  "notes": null
}
```

#### d) Tạo Work Item - Kiểm soát vỏ (Casing Review)

**Request Body**:
```json
{
  "assignmentID": 1,
  "workType": "Casing Review",
  "personName": "Lê Mình Tùng",
  "startDate": null,
  "expectedFinish": null,
  "actualFinish": null,
  "personConfirmation": null,
  "notes": null
}
```

#### e) Tạo Work Item - Định mức vật tư (Material Leveling)

**Request Body**:
```json
{
  "assignmentID": 1,
  "workType": "Material Leveling",
  "personName": "Dương Cộng Hòa",
  "startDate": null,
  "expectedFinish": null,
  "actualFinish": null,
  "personConfirmation": null,
  "notes": null
}
```

**Response (201 Created)**:
```json
{
  "workItemID": 1,
  "assignmentID": 1,
  "workType": "Core Design",
  "personName": "Ngô Cẩm Tú",
  "startDate": null,
  "expectedFinish": null,
  "actualFinish": null,
  "personConfirmation": null,
  "notes": null
}
```

---

### 3. Thêm Work Changes (Các thay đổi công việc)

**Endpoint**: `POST /api/assignments/{assignmentID}/work-changes`

**Headers**:
```http
Content-Type: application/json
Authorization: Bearer {token}
```

**Request Body**:
```json
{
  "assignmentID": 1,
  "changeType": "Change Request",
  "description": "ghfhggh"
}
```

**Response (201 Created)**:
```json
{
  "changeID": 1,
  "assignmentID": 1,
  "changeType": "Change Request",
  "description": "ghfhggh"
}
```

---

## 💻 Ví dụ Code Frontend (TypeScript/Angular)

### Service Example

```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AssignmentService {
  private apiUrl = 'http://localhost:5000/api/assignments';
  
  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('token'); // Lấy token từ storage
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  // Tạo assignment mới
  createAssignment(data: CreateAssignmentDto): Observable<MachineAssignmentDto> {
    return this.http.post<MachineAssignmentDto>(
      this.apiUrl,
      data,
      { headers: this.getHeaders() }
    );
  }

  // Tạo work item
  createWorkItem(assignmentId: number, workItem: CreateWorkItemDto): Observable<WorkItemDto> {
    return this.http.post<WorkItemDto>(
      `${this.apiUrl}/${assignmentId}/work-items`,
      workItem,
      { headers: this.getHeaders() }
    );
  }

  // Tạo work change
  createWorkChange(assignmentId: number, workChange: CreateWorkChangeDto): Observable<WorkChangeDto> {
    return this.http.post<WorkChangeDto>(
      `${this.apiUrl}/${assignmentId}/work-changes`,
      workChange,
      { headers: this.getHeaders() }
    );
  }
}
```

### Component Example

```typescript
import { Component } from '@angular/core';
import { AssignmentService } from './assignment.service';

@Component({
  selector: 'app-create-assignment',
  templateUrl: './create-assignment.component.html'
})
export class CreateAssignmentComponent {
  constructor(private assignmentService: AssignmentService) {}

  async createAssignment(formData: any) {
    try {
      // 1. Tạo assignment chính
      const assignment = await this.assignmentService.createAssignment({
        tbkt_ID: formData.requestDocument || '',
        machineName: formData.machineName,
        standardRequirement: formData.standardRequirement,
        additionalRequest: formData.additionalRequest,
        deliveryDate: formData.deliveryDate ? new Date(formData.deliveryDate).toISOString() : null,
        designer: formData.designer?.toString(),
        teamLeader: formData.teamLeader?.toString()
      }).toPromise();

      if (!assignment) {
        throw new Error('Failed to create assignment');
      }

      const assignmentId = assignment.assignmentID;

      // 2. Tạo các work items
      const workItems = [];

      // Core Design
      if (formData.coreDesignUser) {
        workItems.push(
          this.assignmentService.createWorkItem(assignmentId, {
            assignmentID: assignmentId,
            workType: 'Core Design',
            personName: formData.coreDesignUser.toString(),
            startDate: null,
            expectedFinish: null,
            actualFinish: null,
            personConfirmation: null,
            notes: null
          }).toPromise()
        );
      }

      // Core Review
      if (formData.coreReviewUser) {
        workItems.push(
          this.assignmentService.createWorkItem(assignmentId, {
            assignmentID: assignmentId,
            workType: 'Core Review',
            personName: formData.coreReviewUser.toString(),
            startDate: null,
            expectedFinish: null,
            actualFinish: null,
            personConfirmation: null,
            notes: null
          }).toPromise()
        );
      }

      // Casing Design
      if (formData.casingDesignUser) {
        workItems.push(
          this.assignmentService.createWorkItem(assignmentId, {
            assignmentID: assignmentId,
            workType: 'Casing Design',
            personName: formData.casingDesignUser.toString(),
            startDate: null,
            expectedFinish: null,
            actualFinish: null,
            personConfirmation: null,
            notes: null
          }).toPromise()
        );
      }

      // Casing Review
      if (formData.casingReviewUser) {
        workItems.push(
          this.assignmentService.createWorkItem(assignmentId, {
            assignmentID: assignmentId,
            workType: 'Casing Review',
            personName: formData.casingReviewUser.toString(),
            startDate: null,
            expectedFinish: null,
            actualFinish: null,
            personConfirmation: null,
            notes: null
          }).toPromise()
        );
      }

      // Material Leveling
      if (formData.materialLevelingUser) {
        workItems.push(
          this.assignmentService.createWorkItem(assignmentId, {
            assignmentID: assignmentId,
            workType: 'Material Leveling',
            personName: formData.materialLevelingUser.toString(),
            startDate: null,
            expectedFinish: null,
            actualFinish: null,
            personConfirmation: null,
            notes: null
          }).toPromise()
        );
      }

      // 3. Tạo work change nếu có
      if (formData.workChanges) {
        await this.assignmentService.createWorkChange(assignmentId, {
          assignmentID: assignmentId,
          changeType: 'Change Request',
          description: formData.workChanges
        }).toPromise();
      }

      // 4. Đợi tất cả work items được tạo
      await Promise.all(workItems);

      console.log('Assignment created successfully!', assignment);
      return assignment;

    } catch (error) {
      console.error('Error creating assignment:', error);
      throw error;
    }
  }
}
```

---

## 📝 DTOs (Data Transfer Objects)

### CreateMachineAssignmentDto
```typescript
interface CreateMachineAssignmentDto {
  tbkt_ID: string;              // Required, max 50 chars
  machineName: string;           // Required, max 255 chars
  standardRequirement?: string;  // Optional, max 1000 chars
  additionalRequest?: string;   // Optional, max 1000 chars
  deliveryDate?: string;         // Optional, ISO 8601 format
  designer?: string;             // Optional, max 100 chars
  teamLeader?: string;           // Optional, max 100 chars
}
```

### CreateWorkItemDto
```typescript
interface CreateWorkItemDto {
  assignmentID: number;          // Required
  workType?: string;             // Optional, max 100 chars (e.g., "Core Design", "Casing Review")
  personName?: string;           // Optional, max 100 chars
  startDate?: string;            // Optional, ISO 8601 format
  expectedFinish?: string;       // Optional, ISO 8601 format
  actualFinish?: string;         // Optional, ISO 8601 format
  personConfirmation?: boolean;  // Optional
  notes?: string;                // Optional, max 500 chars
}
```

### CreateWorkChangeDto
```typescript
interface CreateWorkChangeDto {
  assignmentID: number;          // Required
  changeType?: string;           // Optional, max 100 chars
  description?: string;          // Optional, max 1000 chars
}
```

---

## 🔄 Flow Tạo Assignment Hoàn Chỉnh

1. **POST** `/api/assignments` - Tạo assignment chính
   - Nhận `assignmentID` từ response

2. **POST** `/api/assignments/{assignmentID}/work-items` - Tạo từng work item
   - Core Design
   - Core Review
   - Casing Design
   - Casing Review
   - Material Leveling

3. **POST** `/api/assignments/{assignmentID}/work-changes` (nếu có) - Tạo work change

---

## ⚠️ Lưu Ý Quan Trọng

1. **Authentication**: Tất cả API đều yêu cầu JWT Token (trừ login/register)
2. **Content-Type**: Luôn set `Content-Type: application/json`
3. **Date Format**: Sử dụng ISO 8601 format cho dates: `YYYY-MM-DDTHH:mm:ss.sssZ`
4. **Error Handling**: Luôn xử lý lỗi từ API (400, 401, 404, 500)
5. **Work Items**: Phải tạo riêng biệt sau khi assignment được tạo
6. **User IDs**: Các trường như `designer`, `teamLeader`, `personName` có thể là ID (string) hoặc tên (string)

---

## 🧪 Testing với Swagger

Truy cập `http://localhost:5000/swagger` để test các API endpoints trực tiếp.

---

## 📞 Các API Khác

### Authentication
- `POST /api/auth/login` - Đăng nhập
- `POST /api/auth/register` - Đăng ký
- `GET /api/auth/me` - Lấy thông tin user hiện tại

### Users
- `GET /api/users` - Lấy danh sách users
- `GET /api/users/{id}` - Lấy user theo ID
- `POST /api/users` - Tạo user mới
- `PUT /api/users/{id}` - Cập nhật user

### Files & Folders
- `GET /api/files` - Lấy danh sách files
- `POST /api/files` - Upload file
- `GET /api/folders` - Lấy danh sách folders
- `POST /api/folders` - Tạo folder

Xem thêm chi tiết tại: `http://localhost:5000/swagger`

