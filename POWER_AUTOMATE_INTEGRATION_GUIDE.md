# Hướng dẫn tích hợp Power Automate với Approval Workflow

## Tổng quan

Hệ thống Approval Workflow tự động gửi thông tin động đến Power Automate flows để:
1. Trigger approval flow khi có sự kiện mới
2. Gửi email thông báo với thông tin đầy đủ

## Cấu hình

### 1. Cấu hình trong `appsettings.json`

```json
{
  "PowerAutomate": {
    "FlowURL": "https://your-power-automate-flow-url",
    "EmailNotificationFlowURL": "https://your-email-notification-flow-url"
  },
  "AppSettings": {
    "BaseUrl": "http://localhost:4200"
  }
}
```

### 2. Power Automate Flow URLs

- **FlowURL**: URL của flow chính để trigger approval process
- **EmailNotificationFlowURL**: URL của flow để gửi email thông báo
- **BaseUrl**: URL của frontend application (để tạo link trong email)

## Payload Structure

### TriggerApprovalFlowAsync Payload

Khi gọi `TriggerApprovalFlowAsync`, hệ thống sẽ gửi payload sau:

```json
{
  "workflowId": 1,
  "requestTitle": "Yêu cầu ký duyệt Technical Sheet TBKT001",
  "requestDescription": "Cần ký duyệt technical sheet cho dự án mới",
  "requestType": "File",
  "requestReferenceID": "https://thibidi-my.sharepoint.com/...",
  
  "requesterName": "Nguyễn Văn A",
  "requesterEmail": "nguyenvana@example.com",
  "requesterFirebaseUID": "firebase-uid-123",
  "requestSentDate": "2024-01-01T10:00:00Z",
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
  "timestamp": "2024-01-01T10:00:00Z",
  "workflowUrl": "http://localhost:4200/approval-workflow",
  "viewWorkflowUrl": "http://localhost:4200/approval-workflow?workflowId=1"
}
```

### SendNotificationEmailAsync Payload

Khi gọi `SendNotificationEmailAsync`, hệ thống sẽ gửi payload sau:

```json
{
  "toEmail": "controller@example.com",
  "subject": "Yêu cầu ký duyệt: Technical Sheet TBKT001",
  "body": "Bạn có một yêu cầu ký duyệt mới cần được kiểm soát...",
  
  "workflowId": 1,
  "requestTitle": "Yêu cầu ký duyệt Technical Sheet TBKT001",
  "requestDescription": "Cần ký duyệt technical sheet cho dự án mới",
  "requestType": "File",
  "requestReferenceID": "https://thibidi-my.sharepoint.com/...",
  
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
  "timestamp": "2024-01-01T10:00:00Z"
}
```

## Actions

Hệ thống sẽ gửi các action sau tùy theo tình huống:

1. **send_request**: Khi tạo workflow mới, gửi yêu cầu đến kiểm soát
2. **control_approved**: Khi kiểm soát phê duyệt, chuyển đến xét duyệt
3. **control_rejected**: Khi kiểm soát từ chối
4. **approval_completed**: Khi xét duyệt phê duyệt, hoàn tất quy trình
5. **approval_rejected**: Khi xét duyệt từ chối

## Cách sử dụng trong Power Automate

### 1. Tạo Flow nhận HTTP Request

1. Tạo flow mới với trigger "When an HTTP request is received"
2. Định nghĩa JSON schema dựa trên payload structure ở trên
3. Sử dụng các field động để format email

### 2. Format Email với Dynamic Content

Trong Power Automate, bạn có thể sử dụng các field động để tạo email HTML:

**Subject:**
```
@{triggerBody()?['subject']}
```

**Body (HTML):**
```html
<p>Xin chào <strong>@{triggerBody()?['controllerName']}</strong>,</p>
<p>Bạn có một yêu cầu ký duyệt mới cần được kiểm soát:</p>
<table>
  <tr><td><strong>Tiêu đề:</strong></td><td>@{triggerBody()?['requestTitle']}</td></tr>
  <tr><td><strong>Mô tả:</strong></td><td>@{triggerBody()?['requestDescription']}</td></tr>
  <tr><td><strong>Loại:</strong></td><td>@{triggerBody()?['requestType']}</td></tr>
  <tr><td><strong>Mã tham chiếu:</strong></td><td><a href="@{triggerBody()?['requestReferenceID']}">@{triggerBody()?['requestReferenceID']}</a></td></tr>
  <tr><td><strong>Người gửi:</strong></td><td>@{triggerBody()?['requesterName']} (@{triggerBody()?['requesterEmail']})</td></tr>
</table>
<p><a href="@{triggerBody()?['viewWorkflowUrl']}" style="background-color: #1976d2; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;">Xem Chi Tiết</a></p>
```

### 3. Gửi Email đến nhiều người

Nếu cần gửi email đến nhiều người (multiple recipients), bạn có thể:
- Sử dụng field `controllerEmail` và `approverEmail` riêng lẻ
- Hoặc tạo array từ các email trong Power Automate

### 4. Xử lý các Action khác nhau

Trong Power Automate, bạn có thể sử dụng field `action` để xử lý logic khác nhau:

```
Switch action:
- Case "send_request": Gửi email cho controller
- Case "control_approved": Gửi email cho approver
- Case "control_rejected": Gửi email cho requester (từ chối)
- Case "approval_completed": Gửi email cho requester (hoàn tất)
- Case "approval_rejected": Gửi email cho requester (từ chối)
```

## Ví dụ Power Automate Flow

### Flow 1: Email Notification Flow

**Trigger:** HTTP Request
- Method: POST
- Schema: Như payload SendNotificationEmailAsync ở trên

**Action 1:** Compose (Format email body)
```
Xin chào @{triggerBody()?['controllerName']},

Bạn có một yêu cầu ký duyệt mới:

Tiêu đề: @{triggerBody()?['requestTitle']}
Mô tả: @{triggerBody()?['requestDescription']}
Loại: @{triggerBody()?['requestType']}
Mã tham chiếu: @{triggerBody()?['requestReferenceID']}
Người gửi: @{triggerBody()?['requesterName']} (@{triggerBody()?['requesterEmail']})

Xem chi tiết: @{triggerBody()?['viewWorkflowUrl']}
```

**Action 2:** Send an email (V2)
- To: `@{triggerBody()?['toEmail']}`
- Subject: `@{triggerBody()?['subject']}`
- Body: Sử dụng output từ Compose hoặc format HTML với dynamic content

### Flow 2: Approval Flow Trigger

**Trigger:** HTTP Request
- Method: POST
- Schema: Như payload TriggerApprovalFlowAsync ở trên

**Action:** Xử lý logic dựa trên `action` field:
- Log workflow information
- Update external systems
- Send notifications to other systems
- etc.

## Lưu ý

1. **BaseUrl**: Cập nhật `AppSettings:BaseUrl` trong `appsettings.json` với URL thực tế của frontend
2. **Error Handling**: Power Automate flows nên có error handling để xử lý các trường hợp lỗi
3. **Security**: Đảm bảo Power Automate flow URLs được bảo mật (sử dụng sig parameter)
4. **Retry Logic**: Có thể thêm retry logic trong Power Automate nếu cần
5. **Logging**: Log các request đến Power Automate để debug

## Testing

Để test Power Automate integration:

1. Tạo workflow mới từ frontend
2. Kiểm tra Power Automate flow run history
3. Verify email được gửi với đúng thông tin
4. Test các action khác nhau (approve, reject)

