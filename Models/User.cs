using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace quanlyfilesBE.Models;

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? FullName { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? FirebaseUID { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}







