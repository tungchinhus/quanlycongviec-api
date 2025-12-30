namespace quanlyfilesBE.DTOs;

public class PagePermissionDto
{
    public int Id { get; set; }
    public string PageRoute { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class UserPagePermissionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public int PagePermissionId { get; set; }
    public string? PageRoute { get; set; }
    public string? PageName { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class CreatePagePermissionDto
{
    public string PageRoute { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateUserPagePermissionDto
{
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class AssignPagePermissionsDto
{
    public int UserId { get; set; }
    public List<PagePermissionAssignmentDto> Permissions { get; set; } = new();
}

public class PagePermissionAssignmentDto
{
    public int PagePermissionId { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

