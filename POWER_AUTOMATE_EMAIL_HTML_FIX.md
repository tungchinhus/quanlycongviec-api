# Hướng dẫn sửa lỗi Email hiển thị HTML tags thô

## Vấn đề

Email đang hiển thị HTML tags thô như:
```
<p>Xin chào <strong>Chinh Do</strong>,</p>
<table>...
```

Thay vì render HTML đẹp.

## Nguyên nhân

Power Automate action "Send an email (V2)" mặc định gửi email dạng **plain text**, không render HTML.

## Giải pháp

### Bước 1: Bật "Is HTML" trong Power Automate

1. Mở Power Automate flow của bạn
2. Click vào action **"Send an email (V2)"**
3. Click vào **"Show advanced options"** (hoặc **"Advanced options"**)
4. Tìm field **"Is HTML"**
5. Chọn **"Yes"** (hoặc **"True"**)
6. Lưu flow

### Bước 2: Kiểm tra Body field

Đảm bảo Body field chứa HTML đầy đủ (bao gồm `<!DOCTYPE html>`, `<html>`, `<head>`, `<body>` tags).

### Bước 3: Test lại

1. Chạy flow test
2. Kiểm tra email nhận được
3. Email phải hiển thị HTML đẹp, không còn HTML tags thô

## Hình ảnh minh họa

### ❌ SAI - Không bật "Is HTML":
```
Email hiển thị:
<p>Xin chào <strong>Chinh Do</strong>,</p>
<table>...
```

### ✅ ĐÚNG - Bật "Is HTML" = Yes:
```
Email hiển thị:
Xin chào Chinh Do,

[Table đẹp với styling]
[Button đẹp]
```

## Các bước chi tiết trong Power Automate

1. **Mở action "Send an email (V2)"**
   - Click vào action trong flow

2. **Mở Advanced options**
   - Scroll xuống dưới
   - Click vào "Show advanced options" hoặc icon mũi tên xuống

3. **Tìm và bật "Is HTML"**
   - Tìm field "Is HTML"
   - Chọn "Yes" hoặc "True"
   - Field này có thể nằm ở cuối danh sách advanced options

4. **Lưu flow**
   - Click "Save" để lưu thay đổi

## Lưu ý

- **"Is HTML"** phải được bật cho MỖI action "Send an email (V2)" trong flow
- Nếu có nhiều email actions, phải bật cho tất cả
- Sau khi bật, email sẽ render HTML đẹp thay vì hiển thị tags thô

## Template HTML đầy đủ

Đảm bảo bạn copy toàn bộ HTML template này vào Body field:

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

## Kết quả sau khi sửa

Sau khi bật "Is HTML" = Yes:
- ✅ Email sẽ render HTML đẹp
- ✅ Table có styling và border
- ✅ Button có màu sắc và hover effect
- ✅ Link có thể click được
- ✅ Không còn hiển thị HTML tags thô


