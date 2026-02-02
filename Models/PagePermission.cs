using System.ComponentModel.DataAnnotations;
using quanlyfilesBE.Helpers;

namespace quanlyfilesBE.Models;

public class PagePermission
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string PageRoute { get; set; } = string.Empty; // e.g., "/dashboard", "/files", "/assignments"

    [Required]
    [MaxLength(200)]
    public string PageName { get; set; } = string.Empty; // e.g., "Dashboard", "Quản lý File"

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTimeHelper.NowVietnam();

    public ICollection<UserPagePermission> UserPagePermissions { get; set; } = new List<UserPagePermission>();
}

