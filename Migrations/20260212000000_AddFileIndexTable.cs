using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddFileIndexTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileIndex",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullPath = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FolderPath = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Ext = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameNormalized = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Mtime = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileIndex", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileIndex_FolderPath",
                table: "FileIndex",
                column: "FolderPath");

            migrationBuilder.CreateIndex(
                name: "IX_FileIndex_NameNormalized",
                table: "FileIndex",
                column: "NameNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_FileIndex_Ext",
                table: "FileIndex",
                column: "Ext");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileIndex");
        }
    }
}
