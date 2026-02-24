-- Script tạo bảng TiepNhanThongTin (Tiếp nhận thông tin) và HoSoThau (Hồ sơ thầu)
-- SQL Server. Chạy trên database tương ứng (VD: quanlyphancong) nếu chưa dùng EF migration

-- ============================================
-- 1. Bảng TiepNhanThongTin (Tiếp nhận thông tin)
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TiepNhanThongTin')
BEGIN
    CREATE TABLE [dbo].[TiepNhanThongTin] (
        [Id] int IDENTITY(1,1) NOT NULL,
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

    CREATE INDEX [IX_TiepNhanThongTin_SoTNTT] ON [dbo].[TiepNhanThongTin] ([SoTNTT]);
    CREATE INDEX [IX_TiepNhanThongTin_KhachHang] ON [dbo].[TiepNhanThongTin] ([KhachHang]);
    CREATE INDEX [IX_TiepNhanThongTin_NgayNhan] ON [dbo].[TiepNhanThongTin] ([NgayNhan]);
    CREATE INDEX [IX_TiepNhanThongTin_NgayGiao] ON [dbo].[TiepNhanThongTin] ([NgayGiao]);

    PRINT 'Bảng TiepNhanThongTin đã được tạo thành công.';
END
ELSE
BEGIN
    PRINT 'Bảng TiepNhanThongTin đã tồn tại.';
END
GO

-- ============================================
-- 2. Bảng HoSoThau (Hồ sơ thầu)
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HoSoThau')
BEGIN
    CREATE TABLE [dbo].[HoSoThau] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [SoHST] nvarchar(20) NOT NULL,
        [DonViMoiThau] nvarchar(255) NOT NULL,
        [SoTBMTIB] nvarchar(50) NULL,
        [NgayNhan] date NOT NULL,
        [NgayGiaoPhongKD] date NULL,
        [GhiChu] nvarchar(max) NULL,
        CONSTRAINT [PK_HoSoThau] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_HoSoThau_SoHST] UNIQUE ([SoHST])
    );

    CREATE INDEX [IX_HoSoThau_SoTBMTIB] ON [dbo].[HoSoThau] ([SoTBMTIB]);
    CREATE INDEX [IX_HoSoThau_NgayNhan] ON [dbo].[HoSoThau] ([NgayNhan]);
    CREATE INDEX [IX_HoSoThau_DonViMoiThau] ON [dbo].[HoSoThau] ([DonViMoiThau]);

    PRINT 'Bảng HoSoThau đã được tạo thành công.';
END
ELSE
BEGIN
    PRINT 'Bảng HoSoThau đã tồn tại.';
END
GO
