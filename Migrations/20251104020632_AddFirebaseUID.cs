using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddFirebaseUID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirebaseUID",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FirebaseUID",
                table: "Users",
                column: "FirebaseUID",
                unique: true,
                filter: "[FirebaseUID] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_FirebaseUID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FirebaseUID",
                table: "Users");
        }
    }
}
