using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddTSMayTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_MachineAssignment_AssignmentID",
                table: "Files");

            migrationBuilder.CreateTable(
                name: "TSMay",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CongSuat = table.Column<int>(type: "int", nullable: true),
                    SoMay = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SBB = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LSX = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TChuanLSX = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TBKT = table.Column<string>(type: "nchar(10)", maxLength: 10, nullable: true),
                    Po = table.Column<string>(type: "nchar(10)", maxLength: 10, nullable: true),
                    Io = table.Column<string>(type: "nchar(10)", maxLength: 10, nullable: true),
                    Pk75H1 = table.Column<string>(type: "nchar(10)", maxLength: 10, nullable: true),
                    Pk75H2 = table.Column<string>(type: "nchar(10)", maxLength: 10, nullable: true),
                    Uk75H1 = table.Column<string>(type: "nchar(10)", maxLength: 10, nullable: true),
                    Uk75H2 = table.Column<string>(type: "nchar(10)", maxLength: 10, nullable: true),
                    UdmHVH1 = table.Column<string>(type: "nchar(10)", maxLength: 10, nullable: true),
                    UdmHVH2 = table.Column<string>(type: "nchar(10)", maxLength: 10, nullable: true),
                    UdmLV = table.Column<string>(type: "nchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TSMay", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TSMay_CongSuat",
                table: "TSMay",
                column: "CongSuat");

            migrationBuilder.CreateIndex(
                name: "IX_TSMay_LSX",
                table: "TSMay",
                column: "LSX");

            migrationBuilder.CreateIndex(
                name: "IX_TSMay_SBB",
                table: "TSMay",
                column: "SBB");

            migrationBuilder.CreateIndex(
                name: "IX_TSMay_SoMay",
                table: "TSMay",
                column: "SoMay");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_MachineAssignment_AssignmentID",
                table: "Files",
                column: "AssignmentID",
                principalTable: "MachineAssignment",
                principalColumn: "AssignmentID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_MachineAssignment_AssignmentID",
                table: "Files");

            migrationBuilder.DropTable(
                name: "TSMay");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_MachineAssignment_AssignmentID",
                table: "Files",
                column: "AssignmentID",
                principalTable: "MachineAssignment",
                principalColumn: "AssignmentID",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
