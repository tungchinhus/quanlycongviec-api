# Luồng Dữ Liệu: UI Web → Backend → Power Automate Flow

## Tổng quan

Tài liệu này mô tả cách thông tin từ UI web được truyền động vào Power Automate flow.

## 1. Thông tin từ UI Web (Frontend)

### Form Input Fields (approval-workflow-dialog.component.ts)

Khi người dùng tạo workflow mới, các thông tin sau được nhập từ UI:

```typescript
{
  requestTitle: string,           // Tiêu đề yêu cầu
  requestDescription: string,     // Mô tả
  requestType: string,            // Loại yêu cầu (TechnicalSheet, Assignment, WorkItem, File, Other)
  requestReferenceID: string,     // Mã tham chiếu hoặc đường dẫn OneDrive
  controllerEmails: string[],     // Danh sách email người kiểm soát (multi-select)
  approverEmails: string[]        // Danh sách email người xét duyệt (multi-select)
}
```

### DTO gửi đến Backend (CreateApprovalWorkflowDto)

```typescript
{
  requestTitle: string,
  requestDescription: string,
  requestType: string,
  requestReferenceID: string,
  controllerEmail: string,        // Email đầu tiên từ controllerEmails array
  approverEmail: string           // Email đầu tiên từ approverEmails array
}
```

## 2. Backend xử lý và bổ sung thông tin

### ApprovalWorkflowController.Create()

Backend nhận DTO từ frontend và bổ sung thông tin:

#### 2.1. Thông tin người gửi (Requester)
- **Nguồn**: JWT Token từ request
- **Mapping**:
  ```csharp
  RequesterFirebaseUID = currentUserFirebaseUID,  // Từ JWT token
  RequesterName = currentUser?.FullName ?? currentUserName,  // Từ DB hoặc JWT
  RequesterEmail = currentUser?.Email ?? currentUserEmail,   // Từ DB hoặc JWT
  RequestSentDate = DateTime.UtcNow,
  RequestStatus = "Sent"
  ```

#### 2.2. Thông tin kiểm soát (Controller)
- **Nguồn**: Email từ frontend → Tìm trong database
- **Mapping**:
  ```csharp
  ControllerEmail = dto.ControllerEmail,  // Từ frontend
  ControllerName = controllerUser?.FullName,  // Tìm từ DB bằng email
  ControllerFirebaseUID = controllerUser?.FirebaseUID,  // Tìm từ DB bằng email
  ControlStatus = "Pending"
  ```

#### 2.3. Thông tin xét duyệt (Approver)
- **Nguồn**: Email từ frontend → Tìm trong database
- **Mapping**:
  ```csharp
  ApproverEmail = dto.ApproverEmail,  // Từ frontend
  ApproverName = approverUser?.FullName,  // Tìm từ DB bằng email
  ApproverFirebaseUID = approverUser?.FirebaseUID,  // Tìm từ DB bằng email
  ApprovalStatus = "Pending"
  ```

#### 2.4. Thông tin workflow
- **Nguồn**: Trực tiếp từ frontend
- **Mapping**:
  ```csharp
  RequestTitle = dto.RequestTitle,
  RequestDescription = dto.RequestDescription,
  RequestType = dto.RequestType,
  RequestReferenceID = dto.RequestReferenceID,
  OverallStatus = "PendingControl"
  ```

## 3. Truyền vào Power Automate Flow

### 3.1. TriggerApprovalFlowAsync Payload

Sau khi tạo workflow, backend gọi `TriggerApprovalFlowAsync` với payload đầy đủ:

```json
{
  // === Thông tin workflow (từ UI) ===
  "workflowId": 1,
  "requestTitle": "Yêu cầu ký duyệt Technical Sheet TBKT001",  // ← Từ UI
  "requestDescription": "Cần ký duyệt technical sheet...",      // ← Từ UI
  "requestType": "TechnicalSheet",                             // ← Từ UI
  "requestReferenceID": "TBKT001",                             // ← Từ UI
  
  // === Thông tin người gửi (từ JWT + DB) ===
  "requesterName": "Nguyễn Văn A",                             // ← Từ DB
  "requesterEmail": "nguyenvana@example.com",                  // ← Từ DB/JWT
  "requesterFirebaseUID": "firebase-uid-123",                  // ← Từ JWT
  "requestSentDate": "2024-01-01T10:00:00Z",                   // ← Tự động
  "requestStatus": "Sent",                                      // ← Tự động
  
  // === Thông tin kiểm soát (từ UI email → DB) ===
  "controllerName": "Trần Thị B",                              // ← Từ DB (tìm bằng email)
  "controllerEmail": "tranthib@example.com",                    // ← Từ UI
  "controllerFirebaseUID": "firebase-uid-456",                 // ← Từ DB (tìm bằng email)
  "controlStatus": "Pending",                                   // ← Tự động
  "controlReviewDate": "",                                      // ← Chưa có
  "controlNotes": "",                                           // ← Chưa có
  
  // === Thông tin xét duyệt (từ UI email → DB) ===
  "approverName": "Lê Văn C",                                  // ← Từ DB (tìm bằng email)
  "approverEmail": "levanc@example.com",                       // ← Từ UI
  "approverFirebaseUID": "firebase-uid-789",                   // ← Từ DB (tìm bằng email)
  "approvalStatus": "Pending",                                 // ← Tự động
  "approvalDate": "",                                          // ← Chưa có
  "approvalNotes": "",                                         // ← Chưa có
  
  // === Trạng thái tổng thể ===
  "overallStatus": "PendingControl",                           // ← Tự động
  "completedDate": "",                                         // ← Chưa có
  
  // === Action và metadata ===
  "action": "send_request",                                    // ← Tự động
  "timestamp": "2024-01-01T10:00:00Z",                        // ← Tự động
  
  // === URLs để truy cập workflow ===
  "workflowUrl": "http://localhost:4200/approval-workflow",   // ← Từ config
  "viewWorkflowUrl": "http://localhost:4200/approval-workflow?workflowId=1"  // ← Tự động
}
```

### 3.2. SendNotificationEmailAsync Payload

Backend cũng gọi `SendNotificationEmailAsync` để gửi email:

```json
{
  // === Thông tin email ===
  "toEmail": "tranthib@example.com",                          // ← Controller email từ UI
  "subject": "Yêu cầu ký duyệt: Technical Sheet TBKT001",     // ← Tự động tạo
  "body": "Bạn có một yêu cầu ký duyệt mới...",              // ← Tự động tạo
  
  // === Tất cả thông tin workflow (giống như TriggerApprovalFlowAsync) ===
  "workflowId": 1,
  "requestTitle": "...",                                       // ← Từ UI
  "requestDescription": "...",                                // ← Từ UI
  "requestType": "...",                                        // ← Từ UI
  "requestReferenceID": "...",                                 // ← Từ UI
  "requesterName": "...",                                      // ← Từ DB
  "requesterEmail": "...",                                     // ← Từ DB/JWT
  "controllerName": "...",                                     // ← Từ DB
  "controllerEmail": "...",                                    // ← Từ UI
  "approverName": "...",                                       // ← Từ DB
  "approverEmail": "...",                                      // ← Từ UI
  "overallStatus": "...",                                      // ← Tự động
  "workflowUrl": "...",                                        // ← Từ config
  "viewWorkflowUrl": "...",                                    // ← Tự động
  "timestamp": "..."                                           // ← Tự động
}
```

## 4. Sơ đồ luồng dữ liệu

```
┌─────────────────┐
│   UI Web Form   │
│  (Angular)      │
└────────┬────────┘
         │
         │ POST /api/approval-workflow
         │ {
         │   requestTitle, requestDescription,
         │   requestType, requestReferenceID,
         │   controllerEmail, approverEmail
         │ }
         ▼
┌─────────────────────────────────────┐
│   ApprovalWorkflowController        │
│   .Create()                         │
│                                     │
│   1. Lấy thông tin user từ JWT      │
│   2. Tìm controller từ email → DB   │
│   3. Tìm approver từ email → DB     │
│   4. Tạo workflow trong DB          │
└────────┬────────────────────────────┘
         │
         │ workflowDto (đầy đủ thông tin)
         ▼
┌─────────────────────────────────────┐
│   PowerAutomateService              │
│                                     │
│   1. TriggerApprovalFlowAsync()     │
│      → Payload đầy đủ thông tin     │
│                                     │
│   2. SendNotificationEmailAsync()   │
│      → Payload đầy đủ thông tin     │
└────────┬────────────────────────────┘
         │
         │ HTTP POST với JSON payload
         ▼
┌─────────────────────────────────────┐
│   Power Automate Flow                │
│                                     │
│   - Nhận payload động               │
│   - Format email HTML                │
│   - Gửi email với dynamic content   │
└─────────────────────────────────────┘
```

## 5. Đảm bảo thông tin động

### ✅ Tất cả thông tin từ UI đều được truyền:

1. **requestTitle** → `requestTitle` trong payload
2. **requestDescription** → `requestDescription` trong payload
3. **requestType** → `requestType` trong payload
4. **requestReferenceID** → `requestReferenceID` trong payload
5. **controllerEmail** → `controllerEmail` trong payload + tìm `controllerName`, `controllerFirebaseUID`
6. **approverEmail** → `approverEmail` trong payload + tìm `approverName`, `approverFirebaseUID`

### ✅ Thông tin bổ sung tự động:

1. **Requester info**: Từ JWT token và database
2. **Controller/Approver names**: Tìm từ database bằng email
3. **Status fields**: Tự động set theo quy trình
4. **Dates**: Tự động set khi có sự kiện
5. **URLs**: Tự động tạo từ BaseUrl config

## 6. Cách sử dụng trong Power Automate

Trong Power Automate flow, bạn có thể truy cập tất cả thông tin động:

```javascript
// Lấy thông tin từ UI
triggerBody()?['requestTitle']           // Tiêu đề từ UI
triggerBody()?['requestDescription']     // Mô tả từ UI
triggerBody()?['requestType']            // Loại yêu cầu từ UI
triggerBody()?['requestReferenceID']     // Mã tham chiếu từ UI

// Lấy thông tin người gửi
triggerBody()?['requesterName']          // Tên người gửi
triggerBody()?['requesterEmail']         // Email người gửi

// Lấy thông tin kiểm soát
triggerBody()?['controllerName']         // Tên kiểm soát (từ DB)
triggerBody()?['controllerEmail']        // Email kiểm soát (từ UI)

// Lấy thông tin xét duyệt
triggerBody()?['approverName']           // Tên xét duyệt (từ DB)
triggerBody()?['approverEmail']          // Email xét duyệt (từ UI)

// Lấy URLs
triggerBody()?['viewWorkflowUrl']        // Link trực tiếp đến workflow
```

## 7. Ví dụ Email Template trong Power Automate

```html
<p>Xin chào <strong>@{triggerBody()?['controllerName']}</strong>,</p>

<p>Bạn có một yêu cầu ký duyệt mới cần được kiểm soát:</p>

<table border="1" cellpadding="10">
  <tr>
    <td><strong>Tiêu đề:</strong></td>
    <td>@{triggerBody()?['requestTitle']}</td>
  </tr>
  <tr>
    <td><strong>Mô tả:</strong></td>
    <td>@{triggerBody()?['requestDescription']}</td>
  </tr>
  <tr>
    <td><strong>Loại:</strong></td>
    <td>@{triggerBody()?['requestType']}</td>
  </tr>
  <tr>
    <td><strong>Mã tham chiếu:</strong></td>
    <td>
      <a href="@{triggerBody()?['requestReferenceID']}">
        @{triggerBody()?['requestReferenceID']}
      </a>
    </td>
  </tr>
  <tr>
    <td><strong>Người gửi:</strong></td>
    <td>@{triggerBody()?['requesterName']} (@{triggerBody()?['requesterEmail']})</td>
  </tr>
</table>

<p>
  <a href="@{triggerBody()?['viewWorkflowUrl']}" 
     style="background-color: #1976d2; color: white; padding: 10px 20px; 
            text-decoration: none; border-radius: 4px;">
    Xem Chi Tiết và Xử Lý
  </a>
</p>
```

## 8. Kết luận

✅ **Tất cả thông tin từ UI web đều được truyền động vào Power Automate flow:**
- Thông tin nhập từ form (title, description, type, reference)
- Email người kiểm soát và xét duyệt
- Thông tin người dùng (tên, email, firebaseUID) được bổ sung tự động
- URLs để truy cập workflow
- Trạng thái và metadata

✅ **Power Automate có thể sử dụng tất cả thông tin này để:**
- Format email đẹp với dynamic content
- Tạo link trực tiếp đến workflow
- Xử lý logic khác nhau dựa trên action và status
- Gửi thông báo đến đúng người với đầy đủ context


