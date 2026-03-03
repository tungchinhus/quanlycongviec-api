namespace quanlyfilesBE.Models;

/// <summary>
/// Máy sửa chữa - Ghi nhận TNTT máy sửa chữa theo năm (2023-2026).
/// </summary>
public class MaySuaChua
{
    public int Id { get; set; }
    /// <summary>Năm (2023, 2024, 2025, 2026).</summary>
    public int Nam { get; set; }
    /// <summary>Số TNTT/DV/ĐH-P.KD (VD: 08/2026-DV).</summary>
    public string? SoTNTT_DV_DH_PKD { get; set; }
    /// <summary>Thông tin khách hàng.</summary>
    public string? ThongTinKhachHang { get; set; }
    /// <summary>S (kVA).</summary>
    public string? SkVA { get; set; }
    /// <summary>Điện áp.</summary>
    public string? DienAp { get; set; }
    /// <summary>Ngày nhận.</summary>
    public DateTime? NgayNhan { get; set; }
    /// <summary>Người thực hiện.</summary>
    public string? NguoiThucHien { get; set; }
    /// <summary>Số máy.</summary>
    public string? SoMay { get; set; }
    /// <summary>Số TBKT sửa.</summary>
    public string? SoTBKTSua { get; set; }
    /// <summary>Giao P.KD.</summary>
    public string? GiaoPKD { get; set; }
    /// <summary>Ghi chú.</summary>
    public string? GhiChu { get; set; }
}
