# Hướng dẫn cấu hình Power Automate Flow để nhận dữ liệu động từ UI Web

## Vấn đề hiện tại

Power Automate flow đang có schema với hardcoded values:
```json
{
  "to": "chinh.dvt@thibidi.com",
  "subject": "Thông báo phê duyệt",
  "body": "Yêu cầu của bạn đã được phê duyệt."
}
```

## Giải pháp: Cấu hình Dynamic Schema

### Bước 1: Cập nhật Schema trong Power Automate Flow

#### 1.1. Mở Code view của trigger "When an HTTP request is received"

Trong Power Automate flow, click vào trigger "When an HTTP request is received" → Chọn tab **"Code view"**

#### 1.2. Thay thế schema hiện tại bằng schema động

**Xóa schema cũ** (có hardcoded values) và **thay thế bằng schema động** sau:

```json
{
  "type": "Request",
  "kind": "Http",
  "inputs": {
    "triggerAuthenticationType": "All",
    "schema": {
      "type": "object",
      "properties": {
        "toEmail": {
          "type": "string",
          "title": "To Email"
        },
        "subject": {
          "type": "string",
          "title": "Subject"
        },
        "body": {
          "type": "string",
          "title": "Body"
        },
        "workflowId": {
          "type": "integer",
          "title": "Workflow ID"
        },
        "requestTitle": {
          "type": "string",
          "title": "Request Title"
        },
        "requestDescription": {
          "type": "string",
          "title": "Request Description"
        },
        "requestType": {
          "type": "string",
          "title": "Request Type"
        },
        "requestReferenceID": {
          "type": "string",
          "title": "Request Reference ID"
        },
        "requesterName": {
          "type": "string",
          "title": "Requester Name"
        },
        "requesterEmail": {
          "type": "string",
          "title": "Requester Email"
        },
        "controllerName": {
          "type": "string",
          "title": "Controller Name"
        },
        "controllerEmail": {
          "type": "string",
          "title": "Controller Email"
        },
        "controlStatus": {
          "type": "string",
          "title": "Control Status"
        },
        "controlNotes": {
          "type": "string",
          "title": "Control Notes"
        },
        "approverName": {
          "type": "string",
          "title": "Approver Name"
        },
        "approverEmail": {
          "type": "string",
          "title": "Approver Email"
        },
        "approvalStatus": {
          "type": "string",
          "title": "Approval Status"
        },
        "approvalNotes": {
          "type": "string",
          "title": "Approval Notes"
        },
        "overallStatus": {
          "type": "string",
          "title": "Overall Status"
        },
        "workflowUrl": {
          "type": "string",
          "title": "Workflow URL"
        },
        "viewWorkflowUrl": {
          "type": "string",
          "title": "View Workflow URL"
        },
        "timestamp": {
          "type": "string",
          "title": "Timestamp"
        }
      },
      "required": []
    }
  },
  "metadata": {}
}
```

### Bước 2: Sử dụng Dynamic Content trong Action "Send an email (V2)"

Sau khi cập nhật schema, các field sẽ xuất hiện trong Dynamic Content.

#### 2.1. Cấu hình Email Action

1. Click vào action **"Send an email (V2)"**
2. **QUAN TRỌNG**: Trong phần **"Advanced options"**, bật **"Is HTML"** = **Yes** (hoặc **True**)
   - Điều này cho phép email render HTML thay vì hiển thị HTML tags thô
3. Trong các field:

**To:**
```
@{triggerBody()?['toEmail']}
```

**Subject:**
```
@{triggerBody()?['subject']}
```

**Body (HTML) - Đảm bảo "Is HTML" = Yes:**
```html
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <style>
    body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
    .header { background-color: #1976d2; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }
    .content { background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }
    .info-table { width: 100%; border-collapse: collapse; margin: 20px 0; background-color: white; }
    .info-table td { padding: 12px; border: 1px solid #ddd; }
    .info-table td:first-child { background-color: #f0f0f0; font-weight: bold; width: 180px; }
    .button-container { text-align: center; margin: 30px 0; }
    .action-button { background-color: #1976d2; color: white; padding: 14px 28px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold; font-size: 16px; }
    .action-button:hover { background-color: #1565c0; }
    .footer { text-align: center; color: #666; font-size: 12px; margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; }
    .reference-link { color: #1976d2; text-decoration: none; word-break: break-all; }
    .reference-link:hover { text-decoration: underline; }
  </style>
</head>
<body>
  <div class="container">
    <div class="header">
      <h2>🔔 Thông Báo Ký Duyệt</h2>
    </div>
    
    <div class="content">
      <p>Xin chào <strong>@{triggerBody()?['controllerName']}</strong>,</p>
      
      <p>Bạn có một <strong>yêu cầu ký duyệt mới</strong> cần được kiểm soát. Vui lòng xem thông tin chi tiết bên dưới:</p>
      
      <table class="info-table">
        <tr>
          <td>📋 Tiêu đề yêu cầu:</td>
          <td><strong>@{triggerBody()?['requestTitle']}</strong></td>
        </tr>
        <tr>
          <td>📝 Mô tả:</td>
          <td>@{triggerBody()?['requestDescription']}</td>
        </tr>
        <tr>
          <td>🏷️ Loại yêu cầu:</td>
          <td>@{triggerBody()?['requestType']}</td>
        </tr>
        <tr>
          <td>🔗 Mã tham chiếu:</td>
          <td>
            <a href="@{triggerBody()?['requestReferenceID']}" target="_blank" class="reference-link">
              @{triggerBody()?['requestReferenceID']}
            </a>
          </td>
        </tr>
        <tr>
          <td>👤 Người gửi:</td>
          <td>
            <strong>@{triggerBody()?['requesterName']}</strong><br>
            <span style="color: #666; font-size: 12px;">@{triggerBody()?['requesterEmail']}</span>
          </td>
        </tr>
        <tr>
          <td>📊 Trạng thái:</td>
          <td>
            <span style="background-color: #ff9800; color: white; padding: 4px 8px; border-radius: 3px; font-weight: bold;">
              @{triggerBody()?['overallStatus']}
            </span>
          </td>
        </tr>
        <tr>
          <td>📅 Thời gian gửi:</td>
          <td>@{triggerBody()?['timestamp']}</td>
        </tr>
      </table>
      
      <div class="button-container">
        <a href="@{triggerBody()?['viewWorkflowUrl']}" class="action-button">
          🔍 Xem Chi Tiết và Xử Lý
        </a>
      </div>
      
      <p style="background-color: #e3f2fd; padding: 15px; border-left: 4px solid #1976d2; margin: 20px 0;">
        <strong>💡 Lưu ý:</strong> Khi bạn click vào nút "Xem Chi Tiết và Xử Lý" ở trên, hệ thống sẽ tự động mở popup với thông tin chi tiết của yêu cầu này để bạn có thể thực hiện kiểm soát ngay.
      </p>
    </div>
    
    <div class="footer">
      <p>📧 Email này được gửi tự động từ <strong>Hệ Thống Quản Lý Ký Duyệt</strong></p>
      <p>Thời gian: @{triggerBody()?['timestamp']}</p>
      <p style="margin-top: 10px; color: #999;">
        Nếu bạn không phải là người nhận dự kiến, vui lòng bỏ qua email này.
      </p>
    </div>
  </div>
</body>
</html>
```

### Bước 3: Cấu hình cho TriggerApprovalFlowAsync Flow

Nếu bạn có flow riêng cho `TriggerApprovalFlowAsync`, cấu hình tương tự:

#### 3.1. Schema cho TriggerApprovalFlowAsync:

```json
{
  "type": "Request",
  "kind": "Http",
  "inputs": {
    "triggerAuthenticationType": "All",
    "schema": {
      "type": "object",
      "properties": {
        "workflowId": {
          "type": "integer"
        },
        "requestTitle": {
          "type": "string"
        },
        "requestDescription": {
          "type": "string"
        },
        "requestType": {
          "type": "string"
        },
        "requestReferenceID": {
          "type": "string"
        },
        "requesterName": {
          "type": "string"
        },
        "requesterEmail": {
          "type": "string"
        },
        "requesterFirebaseUID": {
          "type": "string"
        },
        "requestSentDate": {
          "type": "string"
        },
        "requestStatus": {
          "type": "string"
        },
        "controllerName": {
          "type": "string"
        },
        "controllerEmail": {
          "type": "string"
        },
        "controllerFirebaseUID": {
          "type": "string"
        },
        "controlStatus": {
          "type": "string"
        },
        "controlReviewDate": {
          "type": "string"
        },
        "controlNotes": {
          "type": "string"
        },
        "approverName": {
          "type": "string"
        },
        "approverEmail": {
          "type": "string"
        },
        "approverFirebaseUID": {
          "type": "string"
        },
        "approvalStatus": {
          "type": "string"
        },
        "approvalDate": {
          "type": "string"
        },
        "approvalNotes": {
          "type": "string"
        },
        "overallStatus": {
          "type": "string"
        },
        "completedDate": {
          "type": "string"
        },
        "action": {
          "type": "string"
        },
        "timestamp": {
          "type": "string"
        },
        "workflowUrl": {
          "type": "string"
        },
        "viewWorkflowUrl": {
          "type": "string"
        }
      }
    }
  }
}
```

## Frontend tự động mở popup từ URL

### Cách hoạt động

1. **Backend tạo URL với query parameter:**
   ```
   http://localhost:4200/approval-workflow?workflowId=1
   ```

2. **Frontend tự động xử lý:**
   - Component `ApprovalWorkflowListComponent` đọc query parameter `workflowId`
   - Tự động load workflow từ API nếu chưa có trong list
   - Tự động mở popup `ApprovalWorkflowActionDialogComponent` với workflow đó
   - Xóa query parameter sau khi mở popup

3. **Code đã được cập nhật:**
   - Component đã import `ActivatedRoute` và `Router`
   - Đã thêm method `openWorkflowById()` để mở workflow theo ID
   - Đã thêm logic trong `ngOnInit()` để đọc query params và tự động mở popup

### Kết quả

Khi người dùng click vào link trong email:
- ✅ Web sẽ mở trang `/approval-workflow`
- ✅ Tự động mở popup với thông tin chi tiết của workflow
- ✅ Người dùng có thể xem và xử lý ngay lập tức

## Cách test

### 1. Test từ Backend

Sau khi cấu hình Power Automate, test bằng cách tạo workflow mới từ UI web.

### 2. Test trực tiếp trong Power Automate

1. Click **"Test"** trong Power Automate flow
2. Chọn **"Manually"**
3. Click **"Run flow"**
4. Nhập test data:
```json
{
  "toEmail": "test@example.com",
  "subject": "Test Email",
  "body": "Test body",
  "workflowId": 1,
  "requestTitle": "Test Request",
  "requestDescription": "Test Description",
  "requestType": "TechnicalSheet",
  "requestReferenceID": "TBKT001",
  "requesterName": "Nguyễn Văn A",
  "requesterEmail": "nguyenvana@example.com",
  "controllerName": "Trần Thị B",
  "controllerEmail": "tranthib@example.com",
  "approverName": "Lê Văn C",
  "approverEmail": "levanc@example.com",
  "overallStatus": "PendingControl",
  "workflowUrl": "http://localhost:4200/approval-workflow",
  "viewWorkflowUrl": "http://localhost:4200/approval-workflow?workflowId=1",
  "timestamp": "2024-01-01T10:00:00Z"
}
```

## Lưu ý quan trọng

1. **Xóa hardcoded values**: Đảm bảo không có giá trị hardcoded trong schema
2. **Dynamic Content**: Luôn sử dụng `@{triggerBody()?['fieldName']}` để lấy giá trị động
3. **Null safety**: Sử dụng `?` operator để tránh lỗi khi field null
4. **HTML Email - QUAN TRỌNG NHẤT**: 
   - **BẮT BUỘC**: Phải bật **"Is HTML" = Yes** trong Advanced options của action "Send an email (V2)"
   - Nếu không bật, email sẽ hiển thị HTML tags thô thay vì render HTML
   - Cách bật: Click vào action → Mở "Advanced options" → Tìm "Is HTML" → Chọn "Yes"
5. **URLs**: Sử dụng `viewWorkflowUrl` để tạo link trực tiếp đến workflow
6. **Email Body**: Copy toàn bộ HTML template (bao gồm cả `<!DOCTYPE html>` và `<html>`, `<head>`, `<body>` tags) vào field Body

## Kết quả

Sau khi cấu hình:
- ✅ Power Automate sẽ nhận dữ liệu động từ backend
- ✅ Email sẽ được format đẹp với HTML template và thông tin đầy đủ từ UI web
- ✅ Mỗi workflow sẽ có email riêng với thông tin chính xác
- ✅ Link trong email (`viewWorkflowUrl`) sẽ dẫn trực tiếp đến workflow với query parameter `?workflowId=X`
- ✅ Frontend tự động mở popup khi có query parameter `workflowId`
- ✅ Người dùng có thể xem và xử lý workflow ngay từ email

## Tóm tắt luồng hoạt động

```
1. Người dùng tạo workflow từ UI web
   ↓
2. Backend tạo workflow và gọi Power Automate
   ↓
3. Power Automate nhận payload động với đầy đủ thông tin
   ↓
4. Power Automate format email HTML đẹp với template
   ↓
5. Email được gửi với link: /approval-workflow?workflowId=X
   ↓
6. Người dùng click link trong email
   ↓
7. Frontend mở trang và tự động mở popup với workflow đó
   ↓
8. Người dùng xem và xử lý workflow ngay lập tức
```

