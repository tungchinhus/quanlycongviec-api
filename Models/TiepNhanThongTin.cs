namespace quanlyfilesBE.Models;

/// <summary>
/// Tiếp nhận thông tin - bảng theo dõi sản phẩm / điện áp (STT, SỐ TNTT, ĐIỆN ÁP, SỐ LƯỢNG, TIÊU CHUẨN, ...).
/// Chỉ cột SỐ TNTT (SoTNTT) là kiểu string; các cột khác giữ kiểu như thiết kế ban đầu.
/// </summary>
public class TiepNhanThongTin
{
    public int Id { get; set; }
    /// <summary>Số TNTT (mã theo dõi) — kiểu string.</summary>
    public string SoTNTT { get; set; } = string.Empty;
    /// <summary>Điện áp - thông số kỹ thuật.</summary>
    public string DienAp { get; set; } = string.Empty;
    /// <summary>Số lượng.</summary>
    public int SoLuong { get; set; }
    /// <summary>Tiêu chuẩn (có thể nhiều, VD: "96,18").</summary>
    public string? TieuChuan { get; set; }
    /// <summary>Phụ kiện kèm theo.</summary>
    public string? PhuKienKemTheo { get; set; }
    /// <summary>Khách hàng.</summary>
    public string KhachHang { get; set; } = string.Empty;
    /// <summary>Ngày nhận.</summary>
    public DateTime NgayNhan { get; set; }
    /// <summary>Ngày giao sản phẩm.</summary>
    public DateTime? NgayGiao { get; set; }
    /// <summary>Ngày lưu (lưu trữ).</summary>
    public DateTime? NgayLuu { get; set; }
    /// <summary>Người thực hiện (P. Kỹ thuật) - Tên.</summary>
    public string? NguoiThucHien { get; set; }
    /// <summary>Ngày giao / Ngày hoàn thành (P. Kỹ thuật).</summary>
    public DateTime? NgayHoanThanh { get; set; }
    /// <summary>Ghi chú.</summary>
    public string? GhiChu { get; set; }
}
