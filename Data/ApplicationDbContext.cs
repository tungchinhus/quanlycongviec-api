using Microsoft.EntityFrameworkCore;
using quanlyfilesBE.Models;

namespace quanlyfilesBE.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<FileItem> Files { get; set; }
    public DbSet<Folder> Folders { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure FileItem
        modelBuilder.Entity<FileItem>(entity =>
        {
            entity.ToTable("Files");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(100);
            entity.Property(e => e.UploadedBy).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.FileName);
            entity.HasIndex(e => e.FileType);
        });

        // Configure Folder
        modelBuilder.Entity<Folder>(entity =>
        {
            entity.ToTable("Folders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FolderName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FolderPath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            
            // Configure self-referencing relationship
            entity.HasOne(e => e.ParentFolder)
                  .WithMany()
                  .HasForeignKey(e => e.ParentFolderId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.FolderName);
            entity.HasIndex(e => e.ParentFolderId);
        });

        // Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => e.FirebaseUID).IsUnique();
        });

        // Roles
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.RoleId);
            entity.HasIndex(e => e.RoleName).IsUnique();
        });

        // Permissions
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(e => e.PermissionId);
            entity.HasIndex(e => e.PermissionName).IsUnique();
        });

        // UserRoles (composite key)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(ur => ur.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ur => ur.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(ur => ur.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // RolePermissions (composite key)
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            entity.HasOne(rp => rp.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(rp => rp.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(rp => rp.Permission)
                  .WithMany(p => p.RolePermissions)
                  .HasForeignKey(rp => rp.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed initial admin role and permission if table is empty at migration time handled separately
    }
}

