namespace quanlyfilesBE.Models;

/// <summary>
/// Enum định nghĩa các role trong hệ thống
/// Đảm bảo đồng nhất giữa Frontend, Backend và Firebase Custom Claims
/// </summary>
public enum RoleType
{
    /// <summary>
    /// Quản trị viên hệ thống - có tất cả quyền
    /// </summary>
    Administrator = 1,

    /// <summary>
    /// Quản lý - có quyền quản lý một số chức năng
    /// </summary>
    Manager = 2,

    /// <summary>
    /// Người dùng thông thường
    /// </summary>
    User = 3,

    /// <summary>
    /// Khách - quyền hạn chế nhất
    /// </summary>
    Guest = 4
}

public static class RoleTypeExtensions
{
    /// <summary>
    /// Chuyển đổi RoleType enum sang string name (để lưu trong DB và Firebase)
    /// </summary>
    public static string ToStringName(this RoleType roleType)
    {
        return roleType switch
        {
            RoleType.Administrator => "Administrator",
            RoleType.Manager => "Manager",
            RoleType.User => "User",
            RoleType.Guest => "Guest",
            _ => throw new ArgumentOutOfRangeException(nameof(roleType), roleType, "Unknown role type - không có fallback hardcode")
        };
    }

    /// <summary>
    /// Chuyển đổi string name sang RoleType enum
    /// </summary>
    public static RoleType? FromStringName(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return null;

        return roleName.Trim() switch
        {
            "Administrator" or "Admin" => RoleType.Administrator,
            "Manager" => RoleType.Manager,
            "User" => RoleType.User,
            "Guest" => RoleType.Guest,
            _ => null
        };
    }

    /// <summary>
    /// Chuyển đổi danh sách RoleType sang danh sách string names
    /// </summary>
    public static List<string> ToStringNames(this IEnumerable<RoleType> roleTypes)
    {
        return roleTypes.Select(rt => rt.ToStringName()).ToList();
    }

    /// <summary>
    /// Chuyển đổi danh sách string names sang danh sách RoleType
    /// </summary>
    public static List<RoleType> FromStringNames(IEnumerable<string>? roleNames)
    {
        if (roleNames == null)
            return new List<RoleType>();

        return roleNames
            .Select(FromStringName)
            .Where(rt => rt.HasValue)
            .Select(rt => rt!.Value)
            .ToList();
    }
}

