using System.ComponentModel.DataAnnotations;

namespace quanlyfilesBE.Models;

public class Role
{
    [Key]
    public int RoleId { get; set; }

    [Required]
    [MaxLength(100)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}







