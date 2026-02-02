using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using quanlyfilesBE.Helpers;

namespace quanlyfilesBE.Models;

public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTimeHelper.NowVietnam();

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}







