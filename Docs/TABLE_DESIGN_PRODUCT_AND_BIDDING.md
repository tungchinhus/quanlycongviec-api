# Gợi ý thiết kế bảng lưu trữ và quản lý

Tài liệu gợi ý thiết kế hai bảng dựa trên dữ liệu hiện có (bảng theo dõi sản phẩm/điện áp và hồ sơ thầu năm 2026). Tên cột: **tiếng Việt không dấu, viết liền** (PascalCase). **Chỉ cột SỐ TNTT (SoTNTT) và SỐ HST (SoHST) là kiểu string; các cột khác giữ kiểu như bảng dưới.**

---

## 1. Tiếp nhận thông tin — `TiepNhanThongTin`

**Mục đích:** Lưu theo dõi sản phẩm (thông số điện áp, số lượng, khách hàng, các ngày nhận/giao/lưu, người thực hiện kỹ thuật, ghi chú).

| Cột DB | Kiểu dữ liệu | Mô tả | Nullable |
|--------|--------------|-------|----------|
| `Id` | `int` PK, IDENTITY | Khóa chính | NOT NULL |
| `SoTNTT` | **`nvarchar(50)`** | **Số TNTT (mã theo dõi) — string** | NOT NULL |
| `DienAp` | `nvarchar(500)` | Điện áp (ĐIỆN ÁP) | NOT NULL |
| `SoLuong` | `int` | Số lượng | NOT NULL |
| `TieuChuan` | `nvarchar(100)` | Tiêu chuẩn (VD: "96,18") | NULL |
| `PhuKienKemTheo` | `nvarchar(255)` | Phụ kiện kèm theo | NULL |
| `KhachHang` | `nvarchar(255)` | Khách hàng | NOT NULL |
| `NgayNhan` | `date` | Ngày nhận | NOT NULL |
| `NgayGiao` | `date` | Ngày giao sản phẩm | NULL |
| `NgayLuu` | `date` | Ngày lưu | NULL |
| `NguoiThucHien` | `nvarchar(255)` | Người thực hiện (P. Kỹ thuật) | NULL |
| `NgayHoanThanh` | `date` | Ngày giao / Ngày hoàn thành | NULL |
| `GhiChu` | `nvarchar(max)` | Ghi chú | NULL |

**Index gợi ý:** `SoTNTT`, `KhachHang`, `NgayNhan`, `NgayGiao`.

---

## 2. Hồ sơ thầu — `HoSoThau`

**Mục đích:** Lưu hồ sơ thầu theo năm (số HST, đơn vị mời thầu, số TBMT IB, ngày nhận, ngày giao phòng KD, ghi chú).

| Cột DB | Kiểu dữ liệu | Mô tả | Nullable |
|--------|--------------|-------|----------|
| `Id` | `int` PK, IDENTITY | Khóa chính | NOT NULL |
| `SoHST` | **`nvarchar(20)`** | **Số HST (VD: 01/2026) — string** | NOT NULL |
| `DonViMoiThau` | `nvarchar(255)` | Đơn vị mời thầu | NOT NULL |
| `SoTBMTIB` | `nvarchar(50)` | Số TBMT IB (VD: IB2500602544) | NULL |
| `NgayNhan` | `date` | Ngày nhận | NOT NULL |
| `NgayGiaoPhongKD` | `date` | Ngày giao phòng kinh doanh | NULL |
| `GhiChu` | `nvarchar(max)` | Ghi chú | NULL |

**Index gợi ý:** `SoHST` (unique), `SoTBMTIB`, `NgayNhan`, `DonViMoiThau`.

---

## 3. Tóm tắt

- **TiepNhanThongTin:** Chỉ **SoTNTT** là string; Id int, SoLuong int, các ngày date, còn lại nvarchar (có cột nullable).
- **HoSoThau:** Chỉ **SoHST** là string; Id int, các ngày date, còn lại nvarchar (có cột nullable).

Các model C# và cấu hình EF nằm trong `Models/` và `Data/ApplicationDbContext.cs`.
