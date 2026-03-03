using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaySuaChua : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaySuaChua",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    SoTNTT_DV_DH_PKD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ThongTinKhachHang = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SkVA = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DienAp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayNhan = table.Column<DateTime>(type: "date", nullable: true),
                    NguoiThucHien = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SoMay = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SoTBKTSua = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GiaoPKD = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaySuaChua", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaySuaChua_Nam",
                table: "MaySuaChua",
                column: "Nam");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaySuaChua");
        }
    }
}
