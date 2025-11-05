using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Models;

namespace quanlyfilesBE.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!await db.Roles.AnyAsync())
        {
            var adminRole = new Role { RoleName = "Admin", Description = "System administrator" };
            var userRole = new Role { RoleName = "User", Description = "Standard user" };
            db.Roles.AddRange(adminRole, userRole);
            await db.SaveChangesAsync();
        }

        if (!await db.Permissions.AnyAsync())
        {
            db.Permissions.AddRange(
                new Permission { PermissionName = "files.view", Description = "View files" },
                new Permission { PermissionName = "files.manage", Description = "Manage files" },
                new Permission { PermissionName = "users.manage", Description = "Manage users" }
            );
            await db.SaveChangesAsync();
        }

        // Ensure Admin role has broad permissions
        var roleAdmin = await db.Roles.FirstAsync(r => r.RoleName == "Admin");
        var allPerms = await db.Permissions.Select(p => p.PermissionId).ToListAsync();
        var existing = await db.RolePermissions.Where(rp => rp.RoleId == roleAdmin.RoleId).Select(rp => rp.PermissionId).ToListAsync();
        var missing = allPerms.Except(existing).ToList();
        foreach (var pid in missing)
        {
            db.RolePermissions.Add(new RolePermission { RoleId = roleAdmin.RoleId, PermissionId = pid });
        }
        if (missing.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }
}





