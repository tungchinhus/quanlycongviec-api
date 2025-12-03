# Thư mục Assets

Thư mục này chứa các file template và tài nguyên tĩnh cho ứng dụng.

## File Template Excel

### thongke_template.xlsx

File template Excel chứa chart để xuất thống kê so sánh thông số.

**Yêu cầu:**
- File phải có worksheet tên "Thống kê"
- Dữ liệu bắt đầu từ dòng 4 (dòng 1-3 là header)
- Chart phải được cấu hình với vùng dữ liệu động (ví dụ: `B4:C1000`) để tự động cập nhật khi có dữ liệu mới
- Các cột dữ liệu:
  - Cột A: Công suất
  - Cột B: TBKT
  - Cột C: Số mẫu
  - Cột D-G: Pk H1 (Max, TB, Min, Delta)
  - Cột H-K: Pk H2 (Max, TB, Min, Delta)
  - Cột L-O: Uk H1 (Max, TB, Min, Delta)
  - Cột P-S: Uk H2 (Max, TB, Min, Delta)

**Lưu ý:**
- File template phải được đặt trong thư mục `assets` ở root của project
- Khi deploy, đảm bảo file template được copy vào thư mục output

