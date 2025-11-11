using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Models;

namespace quanlyfilesBE.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        // CHỈ CHẠY MIGRATION - KHÔNG HARDCODE BẤT KỲ DỮ LIỆU NÀO
        // Tất cả roles, permissions, users phải được quản lý qua API hoặc database trực tiếp
        // Không tự động seed để đảm bảo database là source of truth duy nhất
        
        try
        {
            // Kiểm tra xem database có tồn tại không
            if (await db.Database.CanConnectAsync())
            {
                // Kiểm tra xem có pending migrations không
                var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    Console.WriteLine($"Applying {pendingMigrations.Count()} pending migration(s)...");
                    await db.Database.MigrateAsync();
                    Console.WriteLine("Migrations applied successfully.");
                }
                else
                {
                    Console.WriteLine("Database is up to date. No pending migrations.");
                }
            }
            else
            {
                // Database chưa tồn tại, tạo mới và apply migrations
                Console.WriteLine("Database does not exist. Creating and applying migrations...");
                await db.Database.MigrateAsync();
                Console.WriteLine("Database created and migrations applied successfully.");
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            // Xử lý lỗi SQL - có thể do bảng đã tồn tại hoặc lỗi khác
            Console.WriteLine($"Warning: Database migration encountered an issue: {sqlEx.Message}");
            Console.WriteLine("Application will continue to run. Please check database manually if needed.");
            // Không throw exception để app vẫn có thể chạy
        }
        catch (Exception ex)
        {
            // Xử lý các lỗi khác
            Console.WriteLine($"Warning: An error occurred during database migration: {ex.Message}");
            Console.WriteLine("Application will continue to run. Please check database manually if needed.");
            // Không throw exception để app vẫn có thể chạy
        }

        // KHÔNG TỰ ĐỘNG TẠO ROLES - LẤY TỪ DATABASE
        // Roles phải được tạo thủ công qua API /api/roles hoặc database trực tiếp
        
        // KHÔNG TỰ ĐỘNG TẠO PERMISSIONS - LẤY TỪ DATABASE
        // Permissions phải được tạo thủ công qua API hoặc database trực tiếp
        
        // KHÔNG TỰ ĐỘNG TẠO USERS - LẤY TỪ DATABASE
        // Users phải được tạo thủ công qua API /api/users hoặc database trực tiếp
        
        // KHÔNG TỰ ĐỘNG GÁN PERMISSIONS CHO ROLES - LẤY TỪ DATABASE
        // RolePermissions phải được quản lý qua API /api/roles/{id}/permissions
    }
}



