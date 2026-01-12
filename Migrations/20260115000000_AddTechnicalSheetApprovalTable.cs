using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicalSheetApprovalTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TechnicalSheetApproval",
                columns: table => new
                {
                    ApprovalID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TBKT_ID = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ApprovalLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApproverFirebaseUID = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApproverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalSheetApproval", x => x.ApprovalID);
                    table.ForeignKey(
                        name: "FK_TechnicalSheetApproval_TechnicalSheet_TBKT_ID",
                        column: x => x.TBKT_ID,
                        principalTable: "TechnicalSheet",
                        principalColumn: "TBKT_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalSheetApproval_TBKT_ID",
                table: "TechnicalSheetApproval",
                column: "TBKT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalSheetApproval_ApprovalLevel",
                table: "TechnicalSheetApproval",
                column: "ApprovalLevel");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalSheetApproval_ApprovalStatus",
                table: "TechnicalSheetApproval",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalSheetApproval_ApprovalDate",
                table: "TechnicalSheetApproval",
                column: "ApprovalDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TechnicalSheetApproval");
        }
    }
}
