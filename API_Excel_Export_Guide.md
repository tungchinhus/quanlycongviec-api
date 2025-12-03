# API Excel Export với Chart - Hướng dẫn sử dụng

## Tổng quan

API này cho phép xuất file Excel từ template có sẵn chart, chỉ clear data và fill lại data mới mà không làm mất chart.

## Endpoint

```
POST /api/ExcelExport/export-excel-with-chart
```

## Authentication

Endpoint này yêu cầu authentication. Cần gửi JWT token trong header:

```
Authorization: Bearer <your-jwt-token>
```

## Request Body

```json
{
  "statisticsData": [
    {
      "congSuat": "100kW",
      "tbkt": "TBKT001",
      "soMau": 10,
      "pkH1Max": 1.5,
      "pkH1TB": 1.2,
      "pkH1Min": 1.0,
      "pkH1Delta": 0.5,
      "pkH2Max": 2.0,
      "pkH2TB": 1.8,
      "pkH2Min": 1.5,
      "pkH2Delta": 0.5,
      "ukH1Max": 220.5,
      "ukH1TB": 220.0,
      "ukH1Min": 219.5,
      "ukH1Delta": 1.0,
      "ukH2Max": 221.0,
      "ukH2TB": 220.5,
      "ukH2Min": 220.0,
      "ukH2Delta": 1.0
    }
  ]
}
```

### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| statisticsData | Array | Yes | Mảng các đối tượng thống kê |

### StatisticsData Object Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| congSuat | string | No | Công suất |
| tbkt | string | No | TBKT |
| soMau | number | No | Số mẫu |
| pkH1Max | number | No | Pk H1 Max |
| pkH1TB | number | No | Pk H1 Trung bình |
| pkH1Min | number | No | Pk H1 Min |
| pkH1Delta | number | No | Pk H1 Delta |
| pkH2Max | number | No | Pk H2 Max |
| pkH2TB | number | No | Pk H2 Trung bình |
| pkH2Min | number | No | Pk H2 Min |
| pkH2Delta | number | No | Pk H2 Delta |
| ukH1Max | number | No | Uk H1 Max |
| ukH1TB | number | No | Uk H1 Trung bình |
| ukH1Min | number | No | Uk H1 Min |
| ukH1Delta | number | No | Uk H1 Delta |
| ukH2Max | number | No | Uk H2 Max |
| ukH2TB | number | No | Uk H2 Trung bình |
| ukH2Min | number | No | Uk H2 Min |
| ukH2Delta | number | No | Uk H2 Delta |

## Response

### Success Response

- **Status Code:** 200 OK
- **Content-Type:** `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **Content-Disposition:** `attachment; filename="Thong_ke_so_sanh_thong_so_YYYY-MM-DD_HH-mm-ss.xlsx"`
- **Body:** File Excel binary data

### Error Responses

#### 400 Bad Request
```json
{
  "error": "Dữ liệu thống kê không được để trống"
}
```

#### 404 Not Found
```json
{
  "error": "Không tìm thấy file template",
  "path": "D:\\Project\\...\\assets\\thongke_template.xlsx"
}
```

#### 500 Internal Server Error
```json
{
  "error": "Lỗi khi xuất file Excel",
  "details": "Error message details"
}
```

## Ví dụ sử dụng

### JavaScript/TypeScript (Angular)

```typescript
async exportExcelWithChart(statisticsData: StatisticsDataDto[]): Promise<void> {
  try {
    const response = await this.http.post(
      `${this.apiUrl}/api/ExcelExport/export-excel-with-chart`,
      { statisticsData },
      {
        headers: {
          'Authorization': `Bearer ${this.authService.getToken()}`,
          'Content-Type': 'application/json'
        },
        responseType: 'blob'
      }
    ).toPromise();

    if (!response) {
      throw new Error('No response from server');
    }

    // Tạo blob từ response
    const blob = new Blob([response], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    });

    // Tạo URL và download
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    
    // Lấy tên file từ header hoặc tạo tên mặc định
    const contentDisposition = response.headers?.get('Content-Disposition');
    const fileName = contentDisposition
      ? contentDisposition.split('filename=')[1]?.replace(/"/g, '')
      : `Thong_ke_so_sanh_thong_so_${new Date().toISOString()}.xlsx`;
    
    link.download = fileName;
    link.click();
    window.URL.revokeObjectURL(url);

    this.snackBar.open('Đã xuất thống kê ra file Excel với chart', 'Đóng', {
      duration: 3000,
      horizontalPosition: 'center',
      verticalPosition: 'top'
    });
  } catch (error: any) {
    console.error('Error exporting to Excel:', error);
    this.snackBar.open(
      `Lỗi khi xuất file: ${error.message || 'Unknown error'}`,
      'Đóng',
      {
        duration: 5000,
        horizontalPosition: 'center',
        verticalPosition: 'top'
      }
    );
  }
}
```

### cURL

```bash
curl -X POST "http://localhost:5000/api/ExcelExport/export-excel-with-chart" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "statisticsData": [
      {
        "congSuat": "100kW",
        "tbkt": "TBKT001",
        "soMau": 10,
        "pkH1Max": 1.5,
        "pkH1TB": 1.2,
        "pkH1Min": 1.0,
        "pkH1Delta": 0.5
      }
    ]
  }' \
  --output "thongke.xlsx"
```

## Tính năng Merge Cells

API tự động merge các cell trong cột "Công suất" (cột A) khi có nhiều hàng liên tiếp có cùng giá trị công suất.

**Ví dụ:**
- Nếu hàng 4-17 đều có công suất "250", các cell A4:A17 sẽ được merge thành một cell duy nhất
- Các cell đã merge sẽ được căn giữa theo chiều dọc (vertical center)
- Chỉ merge khi có từ 2 hàng trở lên có cùng công suất

## Lưu ý quan trọng

1. **Template File:** File template `thongke_template.xlsx` phải được đặt trong thư mục `assets` ở root của project.

2. **Chart Configuration:** Trong template Excel, cấu hình chart với vùng dữ liệu động (ví dụ: `B4:C1000`) để chart tự động cập nhật khi có dữ liệu mới.

3. **Worksheet Name:** Template phải có worksheet tên "Thống kê". Nếu không tìm thấy, API sẽ sử dụng worksheet đầu tiên.

4. **Data Start Row:** Dữ liệu được ghi từ dòng 4 (dòng 1-3 là header).

5. **Number Formatting:** Các số sẽ được làm tròn đến 2 chữ số thập phân. Nếu là số nguyên, sẽ không hiển thị phần thập phân.

6. **Merge Cells:** Các hàng liên tiếp có cùng công suất sẽ được tự động merge trong cột A.

7. **EPPlus License:** EPPlus được cấu hình với license NonCommercial. Nếu sử dụng cho mục đích thương mại, cần mua license.

## Troubleshooting

### Lỗi: "Không tìm thấy file template"
- Kiểm tra file `thongke_template.xlsx` có tồn tại trong thư mục `assets` không
- Kiểm tra đường dẫn trong log để xác định vị trí file

### Lỗi: "Worksheet 'Thống kê' not found"
- Kiểm tra tên worksheet trong file template có đúng là "Thống kê" không
- API sẽ tự động sử dụng worksheet đầu tiên nếu không tìm thấy

### Chart bị mất sau khi export
- EPPlus hỗ trợ chart tốt, nhưng một số loại chart phức tạp có thể bị mất
- Đảm bảo chart trong template được cấu hình đúng cách
- Thử với các loại chart cơ bản trước (Line, Column, Bar)

