using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddFileIDToMachineAssignment : Migration
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

            migrationBuilder.AlterColumn<string>(
                name: "PersonConfirmation",
                table: "WorkItem",
                type: "nvarchar(50)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "PersonConfirmation",
                table: "WorkItem",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldNullable: true);

            // Remove File_ID column if it exists (optional - comment out if you want to keep the column)
            // migrationBuilder.Sql(@"
            //     IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[MachineAssignment]') AND name = 'File_ID')
            //     BEGIN
            //         ALTER TABLE [MachineAssignment] DROP COLUMN [File_ID];
            //     END
            // ");
        }
    }
}
