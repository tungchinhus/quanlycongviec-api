using System.ComponentModel.DataAnnotations;

namespace quanlyfilesBE.Models;

public class Permission
{
    [Key]
    public int PermissionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PermissionName { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}







