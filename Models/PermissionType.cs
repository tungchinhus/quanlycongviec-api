namespace quanlyfilesBE.Models;

/// <summary>
/// Enum định nghĩa các permission trong hệ thống
/// Đảm bảo đồng nhất giữa Frontend, Backend và Firebase Custom Claims
/// </summary>
public enum PermissionType
{
    /// <summary>
    /// Quyền xem files
    /// </summary>
    FilesView = 1,

    /// <summary>
    /// Quyền quản lý files (tạo, sửa, xóa)
    /// </summary>
    FilesManage = 2,

    /// <summary>
    /// Quyền quản lý users
    /// </summary>
    UsersManage = 3
}

public static class PermissionTypeExtensions
{
    /// <summary>
    /// Chuyển đổi PermissionType enum sang string name (để lưu trong DB và Firebase)
    /// </summary>
    public static string ToStringName(this PermissionType permissionType)
    {
        return permissionType switch
        {
            PermissionType.FilesView => "files.view",
            PermissionType.FilesManage => "files.manage",
            PermissionType.UsersManage => "users.manage",
            _ => throw new ArgumentOutOfRangeException(nameof(permissionType), permissionType, "Unknown permission type")
        };
    }

    /// <summary>
    /// Chuyển đổi string name sang PermissionType enum
    /// </summary>
    public static PermissionType? FromStringName(string? permissionName)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
            return null;

        return permissionName.Trim() switch
        {
            "files.view" => PermissionType.FilesView,
            "files.manage" => PermissionType.FilesManage,
            "users.manage" => PermissionType.UsersManage,
            _ => null
        };
    }

    /// <summary>
    /// Chuyển đổi danh sách PermissionType sang danh sách string names
    /// </summary>
    public static List<string> ToStringNames(this IEnumerable<PermissionType> permissionTypes)
    {
        return permissionTypes.Select(pt => pt.ToStringName()).ToList();
    }

    /// <summary>
    /// Chuyển đổi danh sách string names sang danh sách PermissionType
    /// </summary>
    public static List<PermissionType> FromStringNames(IEnumerable<string>? permissionNames)
    {
        if (permissionNames == null)
            return new List<PermissionType>();

        return permissionNames
            .Select(FromStringName)
            .Where(pt => pt.HasValue)
            .Select(pt => pt!.Value)
            .ToList();
    }
}

