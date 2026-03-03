using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlyfilesBE.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDesignerToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Chỉ thêm cột nếu chưa tồn tại (tránh lỗi khi đã thêm tay hoặc chạy migration trùng)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'IsDesigner')
                ALTER TABLE [Users] ADD [IsDesigner] bit NOT NULL DEFAULT CAST(0 AS bit);
            ");

            // Chỉ tạo bảng/index khi chưa tồn tại (DB có thể đã có từ trước)
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[dbo].[HoSoThau]', 'U') IS NULL
                CREATE TABLE [HoSoThau] (
                    [Id] int NOT NULL IDENTITY,
                    [SoHST] nvarchar(20) NOT NULL,
                    [DonViMoiThau] nvarchar(255) NOT NULL,
                    [SoTBMTIB] nvarchar(50) NULL,
                    [NgayNhan] date NOT NULL,
                    [NgayGiaoPhongKD] date NULL,
                    [GhiChu] nvarchar(max) NULL,
                    CONSTRAINT [PK_HoSoThau] PRIMARY KEY ([Id])
                );
                IF OBJECT_ID(N'[dbo].[TiepNhanThongTin]', 'U') IS NULL
                CREATE TABLE [TiepNhanThongTin] (
                    [Id] int NOT NULL IDENTITY,
                    [SoTNTT] nvarchar(50) NOT NULL,
                    [DienAp] nvarchar(500) NOT NULL,
                    [SoLuong] int NOT NULL,
                    [TieuChuan] nvarchar(100) NULL,
                    [PhuKienKemTheo] nvarchar(255) NULL,
                    [KhachHang] nvarchar(255) NOT NULL,
                    [NgayNhan] date NOT NULL,
                    [NgayGiao] date NULL,
                    [NgayLuu] date NULL,
                    [NguoiThucHien] nvarchar(255) NULL,
                    [NgayHoanThanh] date NULL,
                    [GhiChu] nvarchar(max) NULL,
                    CONSTRAINT [PK_TiepNhanThongTin] PRIMARY KEY ([Id])
                );
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HoSoThau_DonViMoiThau' AND object_id = OBJECT_ID('HoSoThau'))
                    CREATE INDEX [IX_HoSoThau_DonViMoiThau] ON [HoSoThau] ([DonViMoiThau]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HoSoThau_NgayNhan' AND object_id = OBJECT_ID('HoSoThau'))
                    CREATE INDEX [IX_HoSoThau_NgayNhan] ON [HoSoThau] ([NgayNhan]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HoSoThau_SoHST' AND object_id = OBJECT_ID('HoSoThau'))
                    CREATE UNIQUE INDEX [IX_HoSoThau_SoHST] ON [HoSoThau] ([SoHST]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HoSoThau_SoTBMTIB' AND object_id = OBJECT_ID('HoSoThau'))
                    CREATE INDEX [IX_HoSoThau_SoTBMTIB] ON [HoSoThau] ([SoTBMTIB]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TiepNhanThongTin_KhachHang' AND object_id = OBJECT_ID('TiepNhanThongTin'))
                    CREATE INDEX [IX_TiepNhanThongTin_KhachHang] ON [TiepNhanThongTin] ([KhachHang]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TiepNhanThongTin_NgayGiao' AND object_id = OBJECT_ID('TiepNhanThongTin'))
                    CREATE INDEX [IX_TiepNhanThongTin_NgayGiao] ON [TiepNhanThongTin] ([NgayGiao]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TiepNhanThongTin_NgayNhan' AND object_id = OBJECT_ID('TiepNhanThongTin'))
                    CREATE INDEX [IX_TiepNhanThongTin_NgayNhan] ON [TiepNhanThongTin] ([NgayNhan]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TiepNhanThongTin_SoTNTT' AND object_id = OBJECT_ID('TiepNhanThongTin'))
                    CREATE INDEX [IX_TiepNhanThongTin_SoTNTT] ON [TiepNhanThongTin] ([SoTNTT]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HoSoThau");

            migrationBuilder.DropTable(
                name: "TiepNhanThongTin");

            migrationBuilder.DropColumn(
                name: "IsDesigner",
                table: "Users");
        }
    }
}
