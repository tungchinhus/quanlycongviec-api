namespace quanlyfilesBE.DTOs;

public class RoleDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new List<string>();
}

public class CreateRoleDto
{
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateRoleDto
{
    public string? RoleName { get; set; }
    public string? Description { get; set; }
}

public class AssignPermissionsDto
{
    public List<int> PermissionIds { get; set; } = new List<int>();
}

