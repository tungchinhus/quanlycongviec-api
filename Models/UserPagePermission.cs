using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace quanlyfilesBE.Models;

public class UserPagePermission
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int PagePermissionId { get; set; }

    public bool CanView { get; set; } = true; // Có thể xem page
    public bool CanCreate { get; set; } = false; // Có thể tạo mới
    public bool CanEdit { get; set; } = false; // Có thể chỉnh sửa
    public bool CanDelete { get; set; } = false; // Có thể xóa

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("PagePermissionId")]
    public PagePermission PagePermission { get; set; } = null!;
}

