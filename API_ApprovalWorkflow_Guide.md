# Hướng dẫn sử dụng API Quy trình Ký duyệt 3 cấp

## Tổng quan

Component quy trình ký duyệt 3 cấp được tích hợp với Power Automate để tự động gửi email thông báo:
1. **Cấp 1 - Gửi yêu cầu**: Người dùng gửi yêu cầu ký duyệt
2. **Cấp 2 - Kiểm soát**: Người kiểm soát xem và xác nhận, sau đó chuyển đến người xét duyệt
3. **Cấp 3 - Xét duyệt**: Người xét duyệt xem và phê duyệt hoàn tất quy trình

## Cấu hình Power Automate

Thêm cấu hình vào `appsettings.json`:

```json
{
  "PowerAutomate": {
    "FlowURL": "https://default947b0330c10e4466ba8c8293d24858.f7.environment.api.powerplatform.com:443/powerautomate/automations/direct/workflows/132d5e5722ed45789b399e7a97452144/triggers/manual/paths/invoke?api-version=1&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=EzkfEkAQz7Mxmj4P6Kfek2DpUwbGC1I9m7V8RFbO3qU",
    "EmailNotificationFlowURL": "https://default947b0330c10e4466ba8c8293d24858.f7.environment.api.powerplatform.com:443/powerautomate/automations/direct/workflows/84357c24713a4ed8a5583bb7f3da046c/triggers/manual/paths/invoke?api-version=1&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=XgGXs7D0mch4_PV7xRLN8oCBTIlHD4qHR7J7cXEg0zU"
  }
}
```

### Power Automate Flow cần nhận payload:

**FlowURL** (Trigger approval flow):
```json
{
  "workflowId": 1,
  "requestTitle": "Yêu cầu ký duyệt",
  "requestDescription": "Mô tả yêu cầu",
  "requestType": "TechnicalSheet",
  "requestReferenceID": "TBKT001",
  "requesterName": "Nguyễn Văn A",
  "requesterEmail": "nguyenvana@example.com",
  "requesterFirebaseUID": "firebase-uid-123",
  "requestSentDate": "2024-01-01T00:00:00Z",
  "requestStatus": "Sent",
  "controllerName": "Trần Thị B",
  "controllerEmail": "tranthib@example.com",
  "controllerFirebaseUID": "firebase-uid-456",
  "controlStatus": "Pending",
  "controlReviewDate": "",
  "controlNotes": "",
  "approverName": "Lê Văn C",
  "approverEmail": "levanc@example.com",
  "approverFirebaseUID": "firebase-uid-789",
  "approvalStatus": "Pending",
  "approvalDate": "",
  "approvalNotes": "",
  "overallStatus": "PendingControl",
  "completedDate": "",
  "action": "send_request",
  "timestamp": "2024-01-01T00:00:00Z",
  "workflowUrl": "http://localhost:4200/approval-workflow",
  "viewWorkflowUrl": "http://localhost:4200/approval-workflow?workflowId=1"
}
```

**EmailNotificationFlowURL** (Send email - với thông tin động đầy đủ):
```json
{
  "toEmail": "user@example.com",
  "subject": "Yêu cầu ký duyệt: ...",
  "body": "Nội dung email ngắn gọn...",
  "workflowId": 1,
  "requestTitle": "Yêu cầu ký duyệt",
  "requestDescription": "Mô tả chi tiết",
  "requestType": "TechnicalSheet",
  "requestReferenceID": "TBKT001",
  "requesterName": "Nguyễn Văn A",
  "requesterEmail": "nguyenvana@example.com",
  "controllerName": "Trần Thị B",
  "controllerEmail": "tranthib@example.com",
  "controlStatus": "Pending",
  "controlNotes": "",
  "approverName": "Lê Văn C",
  "approverEmail": "levanc@example.com",
  "approvalStatus": "Pending",
  "approvalNotes": "",
  "overallStatus": "PendingControl",
  "workflowUrl": "http://localhost:4200/approval-workflow",
  "viewWorkflowUrl": "http://localhost:4200/approval-workflow?workflowId=1",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

**Lưu ý:** Power Automate có thể sử dụng các field riêng lẻ để format email HTML đẹp hơn thay vì chỉ dùng field `body`. Ví dụ:
- Sử dụng `requestTitle`, `requestDescription`, `requesterName` để tạo email template
- Sử dụng `viewWorkflowUrl` để tạo button "Xem chi tiết" trong email
- Sử dụng `controlStatus`, `approvalStatus` để hiển thị trạng thái

## API Endpoints

### 1. Tạo quy trình ký duyệt mới

**POST** `/api/approval-workflow`

**Request Body:**
```json
{
  "requestTitle": "Yêu cầu ký duyệt Technical Sheet TBKT001",
  "requestDescription": "Cần ký duyệt technical sheet cho dự án mới",
  "requestType": "TechnicalSheet",
  "requestReferenceID": "TBKT001",
  "controllerEmail": "controller@example.com",
  "approverEmail": "approver@example.com"
}
```

**Response:**
```json
{
  "workflowID": 1,
  "requestTitle": "Yêu cầu ký duyệt Technical Sheet TBKT001",
  "requestDescription": "Cần ký duyệt technical sheet cho dự án mới",
  "requestType": "TechnicalSheet",
  "requestReferenceID": "TBKT001",
  "requesterFirebaseUID": "firebase-uid-123",
  "requesterName": "Nguyễn Văn A",
  "requesterEmail": "nguyenvana@example.com",
  "requestSentDate": "2024-01-01T10:00:00Z",
  "requestStatus": "Sent",
  "controllerFirebaseUID": "firebase-uid-456",
  "controllerName": "Trần Thị B",
  "controllerEmail": "controller@example.com",
  "controlStatus": "Pending",
  "approverFirebaseUID": "firebase-uid-789",
  "approverName": "Lê Văn C",
  "approverEmail": "approver@example.com",
  "approvalStatus": "Pending",
  "overallStatus": "PendingControl",
  "createdAt": "2024-01-01T10:00:00Z"
}
```

**Lưu ý:** Sau khi tạo, hệ thống sẽ tự động:
- Gửi email thông báo cho người kiểm soát
- Trigger Power Automate flow

### 2. Lấy danh sách quy trình ký duyệt

**GET** `/api/approval-workflow?status=PendingControl`

**Query Parameters:**
- `status` (optional): Lọc theo trạng thái (Draft, PendingControl, PendingApproval, Approved, Rejected, Completed)

**Response:**
```json
[
  {
    "workflowID": 1,
    "requestTitle": "Yêu cầu ký duyệt Technical Sheet TBKT001",
    "overallStatus": "PendingControl",
    ...
  }
]
```

### 3. Lấy chi tiết quy trình ký duyệt

**GET** `/api/approval-workflow/{id}`

**Response:** Tương tự như response của POST

### 4. Thực hiện hành động (Kiểm soát hoặc Xét duyệt)

**POST** `/api/approval-workflow/{id}/submit-action`

**Request Body:**
```json
{
  "action": "approve",  // hoặc "reject"
  "notes": "Đã kiểm tra và đồng ý",
  "userRole": "controller"  // hoặc "approver"
}
```

**Lưu ý:**
- Khi `userRole = "controller"` và `action = "approve"`: Hệ thống sẽ tự động gửi email thông báo cho người xét duyệt
- Khi `userRole = "approver"` và `action = "approve"`: Hệ thống sẽ tự động gửi email thông báo hoàn tất cho người gửi
- Khi `action = "reject"`: Hệ thống sẽ gửi email thông báo từ chối cho người gửi

**Response:**
```json
{
  "workflowID": 1,
  "overallStatus": "PendingApproval",  // hoặc "Completed", "Rejected"
  "controlStatus": "Approved",
  "controlReviewDate": "2024-01-01T11:00:00Z",
  "controlNotes": "Đã kiểm tra và đồng ý",
  ...
}
```

### 5. Cập nhật quy trình ký duyệt

**PUT** `/api/approval-workflow/{id}`

**Request Body:**
```json
{
  "requestTitle": "Tiêu đề đã cập nhật",
  "requestDescription": "Mô tả đã cập nhật"
}
```

**Lưu ý:** Chỉ có thể cập nhật khi workflow chưa hoàn tất hoặc bị từ chối.

### 6. Xóa quy trình ký duyệt

**DELETE** `/api/approval-workflow/{id}`

**Lưu ý:** Chỉ có thể xóa khi workflow ở trạng thái Draft hoặc PendingControl.

## Trạng thái (Status)

### RequestStatus
- `Sent`: Đã gửi yêu cầu
- `Cancelled`: Đã hủy

### ControlStatus
- `Pending`: Chờ kiểm soát
- `Approved`: Đã được kiểm soát và chấp nhận
- `Rejected`: Bị từ chối ở cấp kiểm soát

### ApprovalStatus
- `Pending`: Chờ xét duyệt
- `Approved`: Đã được phê duyệt
- `Rejected`: Bị từ chối ở cấp xét duyệt

### OverallStatus
- `Draft`: Nháp (chưa gửi)
- `PendingControl`: Chờ kiểm soát
- `PendingApproval`: Chờ xét duyệt
- `Approved`: Đã được phê duyệt (không dùng)
- `Rejected`: Bị từ chối
- `Completed`: Hoàn tất (đã được phê duyệt ở cả 2 cấp)

## Quy trình hoạt động

1. **Người dùng tạo yêu cầu** → `POST /api/approval-workflow`
   - Status: `PendingControl`
   - Email thông báo tự động gửi cho người kiểm soát

2. **Người kiểm soát xem và xác nhận** → `POST /api/approval-workflow/{id}/submit-action`
   - Nếu approve: Status → `PendingApproval`, Email thông báo gửi cho người xét duyệt
   - Nếu reject: Status → `Rejected`, Email thông báo gửi cho người gửi

3. **Người xét duyệt xem và phê duyệt** → `POST /api/approval-workflow/{id}/submit-action`
   - Nếu approve: Status → `Completed`, Email thông báo hoàn tất gửi cho người gửi
   - Nếu reject: Status → `Rejected`, Email thông báo từ chối gửi cho người gửi

## Phân quyền

- **User thường**: Chỉ xem được workflow của mình hoặc workflow mà họ là controller/approver
- **Admin/Manager**: Xem được tất cả workflow
- Chỉ người gửi hoặc admin mới có thể cập nhật/xóa workflow

## Migration

Sau khi tạo code, cần chạy migration để tạo bảng trong database:

```bash
dotnet ef migrations add AddApprovalWorkflow
dotnet ef database update
```

## Ví dụ sử dụng với Frontend

```typescript
// Tạo quy trình ký duyệt
const createWorkflow = async () => {
  const response = await fetch('/api/approval-workflow', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      requestTitle: 'Yêu cầu ký duyệt Technical Sheet TBKT001',
      requestDescription: 'Cần ký duyệt technical sheet cho dự án mới',
      requestType: 'TechnicalSheet',
      requestReferenceID: 'TBKT001',
      controllerEmail: 'controller@example.com',
      approverEmail: 'approver@example.com'
    })
  });
  return await response.json();
};

// Kiểm soát viên phê duyệt
const approveAsController = async (workflowId: number) => {
  const response = await fetch(`/api/approval-workflow/${workflowId}/submit-action`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      action: 'approve',
      notes: 'Đã kiểm tra và đồng ý',
      userRole: 'controller'
    })
  });
  return await response.json();
};

// Người xét duyệt phê duyệt
const approveAsApprover = async (workflowId: number) => {
  const response = await fetch(`/api/approval-workflow/${workflowId}/submit-action`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      action: 'approve',
      notes: 'Đã xét duyệt và phê duyệt',
      userRole: 'approver'
    })
  });
  return await response.json();
};
```

