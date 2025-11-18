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
    public DbSet<Setting> Settings { get; set; }
    public DbSet<TechnicalSheet> TechnicalSheets { get; set; }
    public DbSet<MachineAssignment> MachineAssignments { get; set; }
    public DbSet<AssignmentApproval> AssignmentApprovals { get; set; }
    public DbSet<WorkChange> WorkChanges { get; set; }
    public DbSet<WorkItem> WorkItems { get; set; }
    public DbSet<TechnicalNotification> TechnicalNotifications { get; set; }
    public DbSet<TSMay> TSMay { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure FileItem
        modelBuilder.Entity<FileItem>(entity =>
        {
            entity.ToTable("Files");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnType("int");
            entity.Property(e => e.AssignmentID)
                .HasColumnType("int");
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileType).HasMaxLength(100);
            entity.Property(e => e.UploadedBy).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            // Configure foreign key relationship with MachineAssignment
            entity.HasOne(e => e.MachineAssignment)
                  .WithMany()
                  .HasForeignKey(e => e.AssignmentID)
                  .OnDelete(DeleteBehavior.Cascade); // Cascade delete - xóa files khi xóa assignment
            
            entity.HasIndex(e => e.FileName);
            entity.HasIndex(e => e.FileType);
            entity.HasIndex(e => e.AssignmentID);
        });

        // Configure Folder
        modelBuilder.Entity<Folder>(entity =>
        {
            entity.ToTable("Folders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnType("int");
            entity.Property(e => e.ParentFolderId)
                .HasColumnType("int");
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
            entity.Property(e => e.UserId)
                .HasColumnType("int");
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => e.FirebaseUID).IsUnique();
        });

        // Roles
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.RoleId)
                .HasColumnType("int");
            entity.HasIndex(e => e.RoleName).IsUnique();
        });

        // Permissions
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(e => e.PermissionId);
            entity.Property(e => e.PermissionId)
                .HasColumnType("int");
            entity.HasIndex(e => e.PermissionName).IsUnique();
        });

        // UserRoles (composite key)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
            entity.Property(ur => ur.UserId)
                .HasColumnType("int");
            entity.Property(ur => ur.RoleId)
                .HasColumnType("int");
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
            entity.Property(rp => rp.RoleId)
                .HasColumnType("int");
            entity.Property(rp => rp.PermissionId)
                .HasColumnType("int");
            entity.HasOne(rp => rp.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(rp => rp.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(rp => rp.Permission)
                  .WithMany(p => p.RolePermissions)
                  .HasForeignKey(rp => rp.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Settings
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.ToTable("Settings");
            entity.HasKey(e => e.SettingId);
            entity.Property(e => e.SettingId)
                .HasColumnType("int");
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // TechnicalSheet
        modelBuilder.Entity<TechnicalSheet>(entity =>
        {
            entity.ToTable("TechnicalSheet");
            entity.HasKey(e => e.TBKT_ID);
            entity.Property(e => e.TBKT_ID).IsRequired().HasMaxLength(50);
            entity.Property(e => e.VoltageSpec).HasMaxLength(100);
            entity.Property(e => e.Phase).HasMaxLength(50);
            entity.Property(e => e.StandardCode).HasMaxLength(100);
            entity.Property(e => e.Proposer).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => e.TBKT_ID);
        });

        // MachineAssignment
        modelBuilder.Entity<MachineAssignment>(entity =>
        {
            entity.ToTable("MachineAssignment");
            entity.HasKey(e => e.AssignmentID);
            entity.Property(e => e.AssignmentID)
                .ValueGeneratedOnAdd()
                .HasColumnType("int");
            entity.Property(e => e.TBKT_ID).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MachineName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.StandardRequirement).HasMaxLength(1000);
            entity.Property(e => e.AdditionalRequest).HasMaxLength(1000);
            entity.Property(e => e.Designer).HasMaxLength(100);
            entity.Property(e => e.TeamLeader).HasMaxLength(100);
            entity.Property(e => e.FilePath).HasMaxLength(4000); // Increased to support multiple file paths separated by semicolon
            entity.Property(e => e.Status)
                .IsRequired()
                .HasDefaultValue(1)
                .HasColumnName("status")
                .HasColumnType("int"); // Map với cột lowercase trong database
            
            entity.HasOne(e => e.TechnicalSheet)
                  .WithMany(ts => ts.MachineAssignments)
                  .HasForeignKey(e => e.TBKT_ID)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.TBKT_ID);
            entity.HasIndex(e => e.MachineName);
        });

        // AssignmentApproval
        modelBuilder.Entity<AssignmentApproval>(entity =>
        {
            entity.ToTable("AssignmentApproval");
            entity.HasKey(e => e.ApprovalID);
            entity.Property(e => e.ApprovalID)
                .ValueGeneratedOnAdd()
                .HasColumnType("int");
            entity.Property(e => e.AssignmentID)
                .HasColumnType("int");
            entity.Property(e => e.ApproverRole).HasMaxLength(100);
            entity.Property(e => e.ApproverName).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            entity.HasOne(e => e.MachineAssignment)
                  .WithMany(ma => ma.AssignmentApprovals)
                  .HasForeignKey(e => e.AssignmentID)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.AssignmentID);
        });

        // WorkChange
        modelBuilder.Entity<WorkChange>(entity =>
        {
            entity.ToTable("WorkChange");
            entity.HasKey(e => e.ChangeID);
            entity.Property(e => e.ChangeID)
                .ValueGeneratedOnAdd()
                .HasColumnType("int");
            entity.Property(e => e.AssignmentID)
                .HasColumnType("int");
            entity.Property(e => e.ChangeType).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            
            entity.HasOne(e => e.MachineAssignment)
                  .WithMany(ma => ma.WorkChanges)
                  .HasForeignKey(e => e.AssignmentID)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.AssignmentID);
        });

        // WorkItem
        modelBuilder.Entity<WorkItem>(entity =>
        {
            entity.ToTable("WorkItem");
            entity.HasKey(e => e.WorkItemID);
            entity.Property(e => e.WorkItemID)
                .ValueGeneratedOnAdd()
                .HasColumnType("int");
            entity.Property(e => e.AssignmentID)
                .HasColumnType("int");
            entity.Property(e => e.WorkType).HasMaxLength(100);
            entity.Property(e => e.PersonName).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            entity.HasOne(e => e.MachineAssignment)
                  .WithMany(ma => ma.WorkItems)
                  .HasForeignKey(e => e.AssignmentID)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.AssignmentID);
        });

        // TechnicalNotification
        modelBuilder.Entity<TechnicalNotification>(entity =>
        {
            entity.ToTable("TechnicalNotification");
            entity.HasKey(e => e.NotificationID);
            entity.Property(e => e.NotificationID)
                .ValueGeneratedOnAdd()
                .HasColumnType("int");
            entity.Property(e => e.TBKT_ID).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DesignReason).HasMaxLength(500);
            entity.Property(e => e.TechnicalStatus).HasMaxLength(100);
            entity.Property(e => e.RoutDrawingCode).HasMaxLength(100);
            entity.Property(e => e.VoDrawingCode).HasMaxLength(100);
            entity.Property(e => e.Accessories).HasMaxLength(500);
            entity.Property(e => e.MaterialUsage).HasMaxLength(500);
            entity.Property(e => e.TechnicalNotes).HasMaxLength(1000);
            entity.Property(e => e.Signer_Proposal).HasMaxLength(100);
            entity.Property(e => e.Signer_Designer).HasMaxLength(100);
            entity.Property(e => e.Signer_Approver).HasMaxLength(100);
            
            entity.HasOne(e => e.TechnicalSheet)
                  .WithMany(ts => ts.TechnicalNotifications)
                  .HasForeignKey(e => e.TBKT_ID)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.TBKT_ID);
        });

        // TSMay
        modelBuilder.Entity<TSMay>(entity =>
        {
            entity.ToTable("TSMay");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnType("int");
            entity.Property(e => e.CongSuat)
                .HasColumnType("int");
            entity.Property(e => e.SoMay)
                .HasMaxLength(50);
            entity.Property(e => e.SBB)
                .HasMaxLength(50);
            entity.Property(e => e.LSX)
                .HasMaxLength(50);
            entity.Property(e => e.TChuanLSX)
                .HasMaxLength(50);
            entity.Property(e => e.TBKT)
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");
            entity.Property(e => e.Po)
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");
            entity.Property(e => e.Io)
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");
            entity.Property(e => e.Pk75H1)
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");
            entity.Property(e => e.Pk75H2)
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");
            entity.Property(e => e.Uk75H1)
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");
            entity.Property(e => e.Uk75H2)
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");
            entity.Property(e => e.UdmHVH1)
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");
            entity.Property(e => e.UdmHVH2)
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");
            entity.Property(e => e.UdmLV)
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");
            
            entity.HasIndex(e => e.SoMay);
            entity.HasIndex(e => e.SBB);
            entity.HasIndex(e => e.LSX);
            entity.HasIndex(e => e.CongSuat);
        });

        // Seed initial admin role and permission if table is empty at migration time handled separately
    }
}

