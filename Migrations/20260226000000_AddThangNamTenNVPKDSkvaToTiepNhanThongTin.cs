using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddThangNamTenNVPKDSkvaToTiepNhanThongTin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThangNam",
                table: "TiepNhanThongTin",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenNVPKD",
                table: "TiepNhanThongTin",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkVA",
                table: "TiepNhanThongTin",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SkVA", table: "TiepNhanThongTin");
            migrationBuilder.DropColumn(name: "TenNVPKD", table: "TiepNhanThongTin");
            migrationBuilder.DropColumn(name: "ThangNam", table: "TiepNhanThongTin");
        }
    }
}
