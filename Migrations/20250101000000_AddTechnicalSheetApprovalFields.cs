using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicalSheetApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ManagerL1 approval fields
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApprovalStatus')
                    ALTER TABLE [TechnicalSheet] ADD [ManagerL1ApprovalStatus] nvarchar(50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApproverFirebaseUID')
                    ALTER TABLE [TechnicalSheet] ADD [ManagerL1ApproverFirebaseUID] nvarchar(200) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApprovalDate')
                    ALTER TABLE [TechnicalSheet] ADD [ManagerL1ApprovalDate] datetime2 NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApprovalNotes')
                    ALTER TABLE [TechnicalSheet] ADD [ManagerL1ApprovalNotes] nvarchar(1000) NULL;
            ");

            // Add Manager approval fields
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApprovalStatus')
                    ALTER TABLE [TechnicalSheet] ADD [ManagerApprovalStatus] nvarchar(50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApproverFirebaseUID')
                    ALTER TABLE [TechnicalSheet] ADD [ManagerApproverFirebaseUID] nvarchar(200) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApprovalDate')
                    ALTER TABLE [TechnicalSheet] ADD [ManagerApprovalDate] datetime2 NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApprovalNotes')
                    ALTER TABLE [TechnicalSheet] ADD [ManagerApprovalNotes] nvarchar(1000) NULL;
            ");

            // Create indexes for better query performance
            migrationBuilder.CreateIndex(
                name: "IX_TechnicalSheet_ManagerL1ApprovalStatus",
                table: "TechnicalSheet",
                column: "ManagerL1ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalSheet_ManagerApprovalStatus",
                table: "TechnicalSheet",
                column: "ManagerApprovalStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TechnicalSheet_ManagerApprovalStatus",
                table: "TechnicalSheet");

            migrationBuilder.DropIndex(
                name: "IX_TechnicalSheet_ManagerL1ApprovalStatus",
                table: "TechnicalSheet");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApprovalStatus')
                    ALTER TABLE [TechnicalSheet] DROP COLUMN [ManagerL1ApprovalStatus];
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApproverFirebaseUID')
                    ALTER TABLE [TechnicalSheet] DROP COLUMN [ManagerL1ApproverFirebaseUID];
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApprovalDate')
                    ALTER TABLE [TechnicalSheet] DROP COLUMN [ManagerL1ApprovalDate];
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApprovalNotes')
                    ALTER TABLE [TechnicalSheet] DROP COLUMN [ManagerL1ApprovalNotes];
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApprovalStatus')
                    ALTER TABLE [TechnicalSheet] DROP COLUMN [ManagerApprovalStatus];
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApproverFirebaseUID')
                    ALTER TABLE [TechnicalSheet] DROP COLUMN [ManagerApproverFirebaseUID];
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApprovalDate')
                    ALTER TABLE [TechnicalSheet] DROP COLUMN [ManagerApprovalDate];
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApprovalNotes')
                    ALTER TABLE [TechnicalSheet] DROP COLUMN [ManagerApprovalNotes];
            ");
        }
    }
}
