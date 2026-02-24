using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddFileIndexCoveringIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Covering index: query chỉ cần FolderPath + Mtime + INCLUDE → không cần đọc bảng, search nhanh hơn.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileIndex_FolderPath_Mtime_Cover' AND object_id = OBJECT_ID('FileIndex'))
                CREATE NONCLUSTERED INDEX IX_FileIndex_FolderPath_Mtime_Cover ON FileIndex (FolderPath, Mtime DESC)
                INCLUDE (Name, FullPath, Ext, NameNormalized);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileIndex_FolderPath_Mtime_Cover' AND object_id = OBJECT_ID('FileIndex'))
                DROP INDEX IX_FileIndex_FolderPath_Mtime_Cover ON FileIndex;
            ");
        }
    }
}
