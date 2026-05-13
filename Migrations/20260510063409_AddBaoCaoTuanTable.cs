using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddBaoCaoTuanTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaoCaoTuan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TuanBaoCao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayLap = table.Column<DateTime>(type: "date", nullable: true),
                    NguoiLap = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CapNhatBoiUserId = table.Column<int>(type: "int", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaoCaoTuan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaoCaoTuan_Users_CapNhatBoiUserId",
                        column: x => x.CapNhatBoiUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_BaoCaoTuan_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoTuan_CapNhatBoiUserId",
                table: "BaoCaoTuan",
                column: "CapNhatBoiUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoTuan_UserId",
                table: "BaoCaoTuan",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoTuan_UserId_TuanBaoCao",
                table: "BaoCaoTuan",
                columns: new[] { "UserId", "TuanBaoCao" },
                unique: true);

            migrationBuilder.Sql("""
                UPDATE b
                SET b.NgayLap = CAST(b.CreatedAt AS date)
                FROM BaoCaoTuan b
                WHERE b.NgayLap IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE b
                SET b.NguoiLap = COALESCE(NULLIF(LTRIM(RTRIM(u.FullName)), ''), NULLIF(LTRIM(RTRIM(u.Email)), ''), NULLIF(LTRIM(RTRIM(u.UserName)), ''))
                FROM BaoCaoTuan b
                INNER JOIN Users u ON u.UserId = b.UserId
                WHERE b.NguoiLap IS NULL OR LTRIM(RTRIM(b.NguoiLap)) = '';
                """);

            migrationBuilder.Sql("""
                UPDATE b
                SET b.CapNhatBoiUserId = b.UserId,
                    b.NguoiCapNhat = COALESCE(NULLIF(LTRIM(RTRIM(u.FullName)), ''), NULLIF(LTRIM(RTRIM(u.Email)), ''), NULLIF(LTRIM(RTRIM(u.UserName)), ''))
                FROM BaoCaoTuan b
                INNER JOIN Users u ON u.UserId = b.UserId
                WHERE b.NguoiCapNhat IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaoCaoTuan");
        }
    }
}
