using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "PersonConfirmation",
                table: "WorkItem",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldNullable: true);

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkItem]') AND name = 'File_ID')
                BEGIN
                    ALTER TABLE [WorkItem] ADD [File_ID] nvarchar(500) NULL;
                END
                ELSE
                BEGIN
                    -- Column exists, just ensure it's the right type
                    ALTER TABLE [WorkItem] ALTER COLUMN [File_ID] nvarchar(500) NULL;
                END
            ");

            migrationBuilder.AlterColumn<string>(
                name: "Proposer",
                table: "TechnicalSheet",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Power_kVA",
                table: "TechnicalSheet",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Phase",
                table: "TechnicalSheet",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            // Drop foreign keys and index, then alter column type for TechnicalSheet.TBKT_ID
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK__MachineAs__TBKT___51300E55')
                    ALTER TABLE [MachineAssignment] DROP CONSTRAINT [FK__MachineAs__TBKT___51300E55];
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK__Technical__TBKT___7EF6D905')
                    ALTER TABLE [TechnicalNotification] DROP CONSTRAINT [FK__Technical__TBKT___7EF6D905];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TechnicalSheet_TBKT_ID' AND object_id = OBJECT_ID('TechnicalSheet'))
                    DROP INDEX [IX_TechnicalSheet_TBKT_ID] ON [TechnicalSheet];
                
                DECLARE @var26 sysname;
                SELECT @var26 = [d].[name]
                FROM [sys].[default_constraints] [d]
                INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
                WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TechnicalSheet]') AND [c].[name] = N'TBKT_ID');
                IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [TechnicalSheet] DROP CONSTRAINT [' + @var26 + '];');
                ALTER TABLE [TechnicalSheet] ALTER COLUMN [TBKT_ID] varchar(50) NOT NULL;
                CREATE INDEX [IX_TechnicalSheet_TBKT_ID] ON [TechnicalSheet] ([TBKT_ID]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ArchivedDate')
                    ALTER TABLE [TechnicalSheet] ADD [ArchivedDate] datetime2 NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'HandOverDate')
                    ALTER TABLE [TechnicalSheet] ADD [HandOverDate] datetime2 NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'RequesterElectrical')
                    ALTER TABLE [TechnicalSheet] ADD [RequesterElectrical] nvarchar(100) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'RequesterMechanical')
                    ALTER TABLE [TechnicalSheet] ADD [RequesterMechanical] nvarchar(100) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'SalesOrder')
                    ALTER TABLE [TechnicalSheet] ADD [SalesOrder] nvarchar(50) NULL;
            ");

            // Alter TBKT_ID for TechnicalNotification
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TechnicalNotification_TBKT_ID' AND object_id = OBJECT_ID('TechnicalNotification'))
                    DROP INDEX [IX_TechnicalNotification_TBKT_ID] ON [TechnicalNotification];
                DECLARE @var27 sysname;
                SELECT @var27 = [d].[name]
                FROM [sys].[default_constraints] [d]
                INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
                WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TechnicalNotification]') AND [c].[name] = N'TBKT_ID');
                IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [TechnicalNotification] DROP CONSTRAINT [' + @var27 + '];');
                ALTER TABLE [TechnicalNotification] ALTER COLUMN [TBKT_ID] varchar(50) NOT NULL;
                CREATE INDEX [IX_TechnicalNotification_TBKT_ID] ON [TechnicalNotification] ([TBKT_ID]);
            ");

            // Alter TBKT_ID for MachineAssignment
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MachineAssignment_TBKT_ID' AND object_id = OBJECT_ID('MachineAssignment'))
                    DROP INDEX [IX_MachineAssignment_TBKT_ID] ON [MachineAssignment];
                DECLARE @var28 sysname;
                SELECT @var28 = [d].[name]
                FROM [sys].[default_constraints] [d]
                INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
                WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MachineAssignment]') AND [c].[name] = N'TBKT_ID');
                IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [MachineAssignment] DROP CONSTRAINT [' + @var28 + '];');
                ALTER TABLE [MachineAssignment] ALTER COLUMN [TBKT_ID] varchar(50) NOT NULL;
                CREATE INDEX [IX_MachineAssignment_TBKT_ID] ON [MachineAssignment] ([TBKT_ID]);
            ");

            // Recreate foreign keys and index
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TechnicalSheet_TBKT_ID' AND object_id = OBJECT_ID('TechnicalSheet'))
                    CREATE INDEX [IX_TechnicalSheet_TBKT_ID] ON [TechnicalSheet] ([TBKT_ID]);
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK__MachineAs__TBKT___51300E55')
                    ALTER TABLE [MachineAssignment] ADD CONSTRAINT [FK__MachineAs__TBKT___51300E55] FOREIGN KEY ([TBKT_ID]) REFERENCES [TechnicalSheet] ([TBKT_ID]);
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK__Technical__TBKT___7EF6D905')
                    ALTER TABLE [TechnicalNotification] ADD CONSTRAINT [FK__Technical__TBKT___7EF6D905] FOREIGN KEY ([TBKT_ID]) REFERENCES [TechnicalSheet] ([TBKT_ID]);
            ");

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflow",
                columns: table => new
                {
                    WorkflowID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequestType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestReferenceID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequesterFirebaseUID = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequesterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequesterEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequestSentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ControllerFirebaseUID = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ControllerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ControllerEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ControlReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ControlStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ControlNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApproverFirebaseUID = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApproverName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApproverEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApprovalNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OverallStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PowerAutomateFlowRunID = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PowerAutomateFlowURL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastNotificationSent = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflow", x => x.WorkflowID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflow_ApproverFirebaseUID",
                table: "ApprovalWorkflow",
                column: "ApproverFirebaseUID");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflow_ControllerFirebaseUID",
                table: "ApprovalWorkflow",
                column: "ControllerFirebaseUID");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflow_OverallStatus",
                table: "ApprovalWorkflow",
                column: "OverallStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflow_RequesterFirebaseUID",
                table: "ApprovalWorkflow",
                column: "RequesterFirebaseUID");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflow_RequestReferenceID",
                table: "ApprovalWorkflow",
                column: "RequestReferenceID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalWorkflow");

            migrationBuilder.DropColumn(
                name: "File_ID",
                table: "WorkItem");

            migrationBuilder.DropColumn(
                name: "ArchivedDate",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "HandOverDate",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "RequesterElectrical",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "RequesterMechanical",
                table: "TechnicalSheet");

            migrationBuilder.DropColumn(
                name: "SalesOrder",
                table: "TechnicalSheet");

            migrationBuilder.AlterColumn<string>(
                name: "PersonConfirmation",
                table: "WorkItem",
                type: "nvarchar(50)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Proposer",
                table: "TechnicalSheet",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Power_kVA",
                table: "TechnicalSheet",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phase",
                table: "TechnicalSheet",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TBKT_ID",
                table: "TechnicalSheet",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "TBKT_ID",
                table: "TechnicalNotification",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "TBKT_ID",
                table: "MachineAssignment",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);
        }
    }
}
