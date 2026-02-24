namespace quanlyfilesBE.Models;

/// <summary>
/// Hồ sơ thầu (SỐ HST, ĐƠN VỊ MỜI THẦU, SỐ TBMT IB, NGÀY NHẬN, NGÀY GIAO P. KD, ...).
/// Chỉ cột SỐ HST (SoHST) là kiểu string; các cột khác giữ kiểu như thiết kế ban đầu.
/// </summary>
public class HoSoThau
{
    public int Id { get; set; }
    /// <summary>Số Hồ sơ thầu (VD: 01/2026, 09/2026) — kiểu string.</summary>
    public string SoHST { get; set; } = string.Empty;
    /// <summary>Đơn vị mời thầu.</summary>
    public string DonViMoiThau { get; set; } = string.Empty;
    /// <summary>Số Thông báo mời thầu IB (VD: IB2500602544).</summary>
    public string? SoTBMTIB { get; set; }
    /// <summary>Ngày nhận hồ sơ.</summary>
    public DateTime NgayNhan { get; set; }
    /// <summary>Ngày giao phòng kinh doanh.</summary>
    public DateTime? NgayGiaoPhongKD { get; set; }
    /// <summary>Ghi chú.</summary>
    public string? GhiChu { get; set; }
}
