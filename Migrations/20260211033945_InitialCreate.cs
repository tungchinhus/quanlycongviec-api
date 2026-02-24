using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add SignaturePath only if not exists (idempotent - tránh lỗi duplicate khi cột đã có từ script thủ công)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'SignaturePath')
                    ALTER TABLE [dbo].[Users] ADD [SignaturePath] nvarchar(500) NULL;
            ");

            // TechnicalSheet Manager* columns: add only if not exists (idempotent - 20250101 có thể đã thêm)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApprovalDate')
                    ALTER TABLE [dbo].[TechnicalSheet] ADD [ManagerApprovalDate] datetime2 NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApprovalNotes')
                    ALTER TABLE [dbo].[TechnicalSheet] ADD [ManagerApprovalNotes] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApprovalStatus')
                    ALTER TABLE [dbo].[TechnicalSheet] ADD [ManagerApprovalStatus] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApproverFirebaseUID')
                    ALTER TABLE [dbo].[TechnicalSheet] ADD [ManagerApproverFirebaseUID] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApprovalDate')
                    ALTER TABLE [dbo].[TechnicalSheet] ADD [ManagerL1ApprovalDate] datetime2 NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApprovalNotes')
                    ALTER TABLE [dbo].[TechnicalSheet] ADD [ManagerL1ApprovalNotes] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApprovalStatus')
                    ALTER TABLE [dbo].[TechnicalSheet] ADD [ManagerL1ApprovalStatus] nvarchar(max) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApproverFirebaseUID')
                    ALTER TABLE [dbo].[TechnicalSheet] ADD [ManagerL1ApproverFirebaseUID] nvarchar(max) NULL;
            ");

            // MachineAssignment: add only if not exists (idempotent - tránh duplicate column)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MachineAssignment]') AND name = 'IsLocked')
                    ALTER TABLE [dbo].[MachineAssignment] ADD [IsLocked] bit NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MachineAssignment]') AND name = 'RequestDocument')
                    ALTER TABLE [dbo].[MachineAssignment] ADD [RequestDocument] nvarchar(255) NULL;
            ");

            // PagePermissions, TechnicalSheetApproval, UserPagePermissions: create only if not exists (idempotent)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[PagePermissions]'))
                BEGIN
                    CREATE TABLE [dbo].[PagePermissions] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [PageRoute] nvarchar(200) NOT NULL,
                        [PageName] nvarchar(200) NOT NULL,
                        [Description] nvarchar(500) NULL,
                        [IsActive] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_PagePermissions] PRIMARY KEY ([Id])
                    );
                END
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheetApproval]'))
                BEGIN
                    CREATE TABLE [dbo].[TechnicalSheetApproval] (
                        [ApprovalID] int NOT NULL IDENTITY(1,1),
                        [TBKT_ID] varchar(50) NOT NULL,
                        [ApprovalLevel] nvarchar(20) NOT NULL,
                        [ApprovalStatus] nvarchar(20) NOT NULL,
                        [ApproverFirebaseUID] nvarchar(200) NOT NULL,
                        [ApproverName] nvarchar(100) NOT NULL,
                        [ApprovalDate] datetime2 NOT NULL,
                        [Notes] nvarchar(1000) NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [PK_TechnicalSheetApproval] PRIMARY KEY ([ApprovalID]),
                        CONSTRAINT [FK_TechnicalSheetApproval_TechnicalSheet_TBKT_ID] FOREIGN KEY ([TBKT_ID]) REFERENCES [dbo].[TechnicalSheet]([TBKT_ID]) ON DELETE CASCADE
                    );
                END
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[UserPagePermissions]'))
                BEGIN
                    CREATE TABLE [dbo].[UserPagePermissions] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [UserId] int NOT NULL,
                        [PagePermissionId] int NOT NULL,
                        [CanView] bit NOT NULL,
                        [CanCreate] bit NOT NULL,
                        [CanEdit] bit NOT NULL,
                        [CanDelete] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_UserPagePermissions] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_UserPagePermissions_PagePermissions_PagePermissionId] FOREIGN KEY ([PagePermissionId]) REFERENCES [dbo].[PagePermissions]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_UserPagePermissions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]) ON DELETE CASCADE
                    );
                END
            ");
            // Indexes: create only if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[PagePermissions]') AND name = 'IX_PagePermissions_PageRoute')
                    CREATE UNIQUE INDEX [IX_PagePermissions_PageRoute] ON [dbo].[PagePermissions]([PageRoute]);
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheetApproval]') AND name = 'IX_TechnicalSheetApproval_ApprovalDate')
                    CREATE INDEX [IX_TechnicalSheetApproval_ApprovalDate] ON [dbo].[TechnicalSheetApproval]([ApprovalDate]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheetApproval]') AND name = 'IX_TechnicalSheetApproval_ApprovalLevel')
                    CREATE INDEX [IX_TechnicalSheetApproval_ApprovalLevel] ON [dbo].[TechnicalSheetApproval]([ApprovalLevel]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheetApproval]') AND name = 'IX_TechnicalSheetApproval_ApprovalStatus')
                    CREATE INDEX [IX_TechnicalSheetApproval_ApprovalStatus] ON [dbo].[TechnicalSheetApproval]([ApprovalStatus]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheetApproval]') AND name = 'IX_TechnicalSheetApproval_TBKT_ID')
                    CREATE INDEX [IX_TechnicalSheetApproval_TBKT_ID] ON [dbo].[TechnicalSheetApproval]([TBKT_ID]);
            ");
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[UserPagePermissions]') AND name = 'IX_UserPagePermissions_PagePermissionId')
                    CREATE INDEX [IX_UserPagePermissions_PagePermissionId] ON [dbo].[UserPagePermissions]([PagePermissionId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[UserPagePermissions]') AND name = 'IX_UserPagePermissions_UserId_PagePermissionId')
                    CREATE UNIQUE INDEX [IX_UserPagePermissions_UserId_PagePermissionId] ON [dbo].[UserPagePermissions]([UserId], [PagePermissionId]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TechnicalSheetApproval");

            migrationBuilder.DropTable(
                name: "UserPagePermissions");

            migrationBuilder.DropTable(
                name: "PagePermissions");

            migrationBuilder.DropColumn(
                name: "SignaturePath",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ManagerApprovalDate",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "ManagerApprovalNotes",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "ManagerApprovalStatus",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "ManagerApproverFirebaseUID",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "ManagerL1ApprovalDate",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "ManagerL1ApprovalNotes",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "ManagerL1ApprovalStatus",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "ManagerL1ApproverFirebaseUID",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "MachineAssignment");

            migrationBuilder.DropColumn(
                name: "RequestDocument",
                table: "MachineAssignment");
        }
    }
}
