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
            // Sử dụng enum để đảm bảo đồng nhất
            var roles = new[]
            {
                new Role { RoleName = RoleType.Administrator.ToStringName(), Description = "System administrator" },
                new Role { RoleName = RoleType.Manager.ToStringName(), Description = "Manager role" },
                new Role { RoleName = RoleType.User.ToStringName(), Description = "Standard user" },
                new Role { RoleName = RoleType.Guest.ToStringName(), Description = "Guest user" }
            };
            db.Roles.AddRange(roles);
            await db.SaveChangesAsync();
        }

        if (!await db.Permissions.AnyAsync())
        {
            // Sử dụng enum để đảm bảo đồng nhất
            var permissions = new[]
            {
                new Permission { PermissionName = PermissionType.FilesView.ToStringName(), Description = "View files" },
                new Permission { PermissionName = PermissionType.FilesManage.ToStringName(), Description = "Manage files" },
                new Permission { PermissionName = PermissionType.UsersManage.ToStringName(), Description = "Manage users" }
            };
            db.Permissions.AddRange(permissions);
            await db.SaveChangesAsync();
        }

        // Ensure Administrator role has broad permissions
        var roleAdmin = await db.Roles.FirstAsync(r => r.RoleName == RoleType.Administrator.ToStringName());
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

        // Seed default Admin user if none exists
        if (!await db.Users.AnyAsync())
        {
            var adminRole = await db.Roles.FirstAsync(r => r.RoleName == RoleType.Administrator.ToStringName());

            var adminUser = new User
            {
                UserName = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FullName = "System Administrator",
                Email = "admin@example.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(adminUser);
            await db.SaveChangesAsync();

            db.UserRoles.Add(new UserRole
            {
                UserId = adminUser.UserId,
                RoleId = adminRole.RoleId,
                AssignedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }
    }
}





