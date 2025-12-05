using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddFileIDToWorkItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add File_ID column to MachineAssignment if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MachineAssignment]') AND name = 'File_ID')
                BEGIN
                    ALTER TABLE [MachineAssignment] ADD [File_ID] int NULL;
                END
            ");

            // Add File_ID column to WorkItem if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkItem]') AND name = 'File_ID')
                BEGIN
                    ALTER TABLE [WorkItem] ADD [File_ID] int NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove File_ID column from WorkItem if it exists (optional - comment out if you want to keep the column)
            // migrationBuilder.Sql(@"
            //     IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkItem]') AND name = 'File_ID')
            //     BEGIN
            //         ALTER TABLE [WorkItem] DROP COLUMN [File_ID];
            //     END
            // ");

            // Remove File_ID column from MachineAssignment if it exists (optional - comment out if you want to keep the column)
            // migrationBuilder.Sql(@"
            //     IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MachineAssignment]') AND name = 'File_ID')
            //     BEGIN
            //         ALTER TABLE [MachineAssignment] DROP COLUMN [File_ID];
            //     END
            // ");
        }
    }
}
