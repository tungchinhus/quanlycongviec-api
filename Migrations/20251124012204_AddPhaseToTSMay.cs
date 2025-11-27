using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddPhaseToTSMay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phase",
                table: "TSMay",
                type: "nchar(1)",
                maxLength: 1,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TSMay_Phase",
                table: "TSMay",
                column: "Phase");

            // Add CHECK constraint to ensure Phase is only '1' or '3'
            migrationBuilder.Sql(@"
                ALTER TABLE TSMay
                ADD CONSTRAINT CK_TSMay_Phase CHECK (Phase IS NULL OR Phase IN ('1', '3'))
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop CHECK constraint first
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_TSMay_Phase')
                BEGIN
                    ALTER TABLE TSMay DROP CONSTRAINT CK_TSMay_Phase
                END
            ");

            migrationBuilder.DropIndex(
                name: "IX_TSMay_Phase",
                table: "TSMay");

            migrationBuilder.DropColumn(
                name: "Phase",
                table: "TSMay");
        }
    }
}
