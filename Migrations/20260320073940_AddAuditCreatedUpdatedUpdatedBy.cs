using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditCreatedUpdatedUpdatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "WorkItem",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "WorkItem",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "WorkItem",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "WorkChange",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "WorkChange",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "WorkChange",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserRoles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserRoles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "UserRoles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "UserPagePermissions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TSMay",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TSMay",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TSMay",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TiepNhanThongTin",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TiepNhanThongTin",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TiepNhanThongTin",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TechnicalSheetApproval",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TechnicalSheetApproval",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TechnicalSheet",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TechnicalSheet",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TechnicalSheet",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TechnicalNotification",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TechnicalNotification",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TechnicalNotification",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Settings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Roles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Roles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Roles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "RolePermissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "RolePermissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "RolePermissions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Permissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Permissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Permissions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PagePermissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "PagePermissions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Notifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Notifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MaySuaChua",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MaySuaChua",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "MaySuaChua",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MachineAssignment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MachineAssignment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "MachineAssignment",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "HoSoThau",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "HoSoThau",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "HoSoThau",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.Folders', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Folders', 'CreatedAt') IS NULL
        ALTER TABLE [dbo].[Folders] ADD [CreatedAt] datetime2 NULL;
    IF COL_LENGTH('dbo.Folders', 'UpdatedAt') IS NULL
        ALTER TABLE [dbo].[Folders] ADD [UpdatedAt] datetime2 NULL;
    IF COL_LENGTH('dbo.Folders', 'UpdatedBy') IS NULL
        ALTER TABLE [dbo].[Folders] ADD [UpdatedBy] nvarchar(200) NULL;
END
");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Files",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Files",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Files",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FileIndex",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FileIndex",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "FileIndex",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AssignmentApproval",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AssignmentApproval",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "AssignmentApproval",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "WorkItem");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "WorkItem");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "WorkItem");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "WorkChange");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "WorkChange");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "WorkChange");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "UserPagePermissions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TSMay");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TSMay");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TSMay");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TiepNhanThongTin");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TiepNhanThongTin");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TiepNhanThongTin");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TechnicalSheetApproval");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TechnicalSheetApproval");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TechnicalNotification");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TechnicalNotification");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TechnicalNotification");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PagePermissions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PagePermissions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MaySuaChua");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MaySuaChua");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "MaySuaChua");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MachineAssignment");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MachineAssignment");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "MachineAssignment");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "HoSoThau");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "HoSoThau");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "HoSoThau");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FileIndex");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FileIndex");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "FileIndex");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AssignmentApproval");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AssignmentApproval");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AssignmentApproval");
        }
    }
}
