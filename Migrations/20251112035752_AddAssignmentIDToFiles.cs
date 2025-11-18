using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentIDToFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignmentID",
                table: "Files",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Files_AssignmentID",
                table: "Files",
                column: "AssignmentID");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_MachineAssignment_AssignmentID",
                table: "Files",
                column: "AssignmentID",
                principalTable: "MachineAssignment",
                principalColumn: "AssignmentID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_MachineAssignment_AssignmentID",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_AssignmentID",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "AssignmentID",
                table: "Files");
        }
    }
}
