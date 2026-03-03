namespace quanlyfilesBE.DTOs;

public class UserDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? FirebaseUID { get; set; }
    public bool IsActive { get; set; }
    /// <summary>True nếu user là nhân viên thiết kế.</summary>
    public bool IsDesigner { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
}

public class UserCreateDto
{
    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? FirebaseUID { get; set; }
    public string? Password { get; set; }
    public List<int>? RoleIds { get; set; }
}

public class UserUpdateDto
{
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public bool? IsActive { get; set; }
    /// <summary>True/False để đánh dấu user là nhân viên thiết kế hay không.</summary>
    public bool? IsDesigner { get; set; }
    public List<int>? RoleIds { get; set; }
}

public class CreateUserWithFirebaseDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new List<string>();
}

public class UpdateUserRolesDto
{
    public List<string> Roles { get; set; } = new List<string>();
}

public class SetCustomClaimsDto
{
    public List<string> Roles { get; set; } = new List<string>();
    public string? Name { get; set; }
}

public class SyncUserFromFirebaseDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new List<string>();
}

public class UserCreateSimpleDto
{
    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; } // Single role name (e.g., "User", "Admin", "Manager")
    public List<string>? Roles { get; set; } // Array of role names (e.g., ["User"], ["Admin"], ["User", "Manager"])
}

