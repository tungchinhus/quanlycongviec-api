using System.Security.Claims;

namespace quanlyfilesBE.Models;

/// <summary>
/// Helper class để kiểm tra roles từ ClaimsPrincipal
/// </summary>
public static class RoleHelper
{
    /// <summary>
    /// Kiểm tra xem user có role Administrator không
    /// </summary>
    public static bool IsAdministrator(ClaimsPrincipal? user)
    {
        if (user == null) return false;
        var adminRoleName = RoleType.Administrator.ToStringName();
        return user.IsInRole(adminRoleName) || user.IsInRole("Admin"); // Support both "Administrator" and "Admin"
    }

    /// <summary>
    /// Kiểm tra xem user có role Manager không
    /// </summary>
    public static bool IsManager(ClaimsPrincipal? user)
    {
        if (user == null) return false;
        return user.IsInRole(RoleType.Manager.ToStringName());
    }

    /// <summary>
    /// Kiểm tra xem user có role User không
    /// </summary>
    public static bool IsUser(ClaimsPrincipal? user)
    {
        if (user == null) return false;
        return user.IsInRole(RoleType.User.ToStringName());
    }

    /// <summary>
    /// Kiểm tra xem user có role Guest không
    /// </summary>
    public static bool IsGuest(ClaimsPrincipal? user)
    {
        if (user == null) return false;
        return user.IsInRole(RoleType.Guest.ToStringName());
    }

    /// <summary>
    /// Kiểm tra xem user có role Administrator hoặc Manager không
    /// </summary>
    public static bool IsAdministratorOrManager(ClaimsPrincipal? user)
    {
        return IsAdministrator(user) || IsManager(user);
    }

    /// <summary>
    /// Kiểm tra xem user có quyền full (admin) - Admin có quyền bypass tất cả permission checks
    /// </summary>
    public static bool HasFullPermissions(ClaimsPrincipal? user)
    {
        return IsAdministrator(user);
    }

    /// <summary>
    /// Kiểm tra xem user có permission cụ thể không, hoặc là admin (admin có full permissions)
    /// </summary>
    public static bool HasPermission(ClaimsPrincipal? user, string permissionName)
    {
        if (HasFullPermissions(user))
            return true; // Admin có full permissions
        
        if (user == null) return false;
        
        // Kiểm tra permission từ claims
        return user.HasClaim("permission", permissionName);
    }

    /// <summary>
    /// Lấy role name string để dùng trong [Authorize(Roles = "...")]
    /// </summary>
    public static class AuthorizeRoles
    {
        public const string Administrator = "Administrator";
        public const string Manager = "Manager";
        public const string User = "User";
        public const string Guest = "Guest";
        
        // Support legacy "Admin" name
        public const string Admin = "Administrator";
        
        // All roles string for endpoints that should allow all authenticated users
        public const string AllRoles = "Administrator,Admin,Manager,User,Guest";
    }
}

