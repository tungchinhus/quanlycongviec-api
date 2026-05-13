using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace quanlyfilesBE.Models;

/// <summary>Báo cáo công tác tuần — mỗi user quản lý bản ghi của chính mình.</summary>
public class BaoCaoTuan
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    /// <summary>Khoảng tuần, ví dụ: 20/04-24/04/2026</summary>
    [Required]
    [MaxLength(100)]
    public string TuanBaoCao { get; set; } = string.Empty;

    /// <summary>Ngày lập báo cáo — do server gán khi tạo (VN), không nhận từ client.</summary>
    public DateTime? NgayLap { get; set; }

    /// <summary>Người lập — do server gán từ user tạo, không nhận từ client.</summary>
    [MaxLength(200)]
    public string? NguoiLap { get; set; }

    /// <summary>JSON: mảng các dòng (stt, danhMuc1, danhMuc2, ketQua, tongSo, vuongMac, deXuat).</summary>
    [Required]
    public string RowsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>User thực hiện lần lưu gần nhất (tạo hoặc cập nhật).</summary>
    public int? CapNhatBoiUserId { get; set; }

    [ForeignKey(nameof(CapNhatBoiUserId))]
    public User? CapNhatBoiUser { get; set; }

    /// <summary>Người cập nhật gần nhất — do server gán khi tạo/sửa, không nhận từ client.</summary>
    [MaxLength(200)]
    public string? NguoiCapNhat { get; set; }
}
