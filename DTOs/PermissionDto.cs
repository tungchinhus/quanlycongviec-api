namespace quanlyfilesBE.DTOs;

public class PermissionDto
{
    public int PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreatePermissionDto
{
    public string PermissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdatePermissionDto
{
    public string? PermissionName { get; set; }
    public string? Description { get; set; }
}

