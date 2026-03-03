-- Script thêm 3 cột ThangNam, TenNVPKD, SkVA vào bảng TiepNhanThongTin (Tiếp nhận thông tin)
-- SQL Server. Chạy trên database (VD: quanlyphancong) — chọn đúng database trước khi F5.
-- Có thể chạy nhiều lần (idempotent).

-- Tháng/Năm (VD: 12/2025, 01/2026)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TiepNhanThongTin]') AND name = 'ThangNam')
BEGIN
    ALTER TABLE [dbo].[TiepNhanThongTin] ADD [ThangNam] nvarchar(20) NULL;
    PRINT 'Đã thêm cột ThangNam.';
END
ELSE
    PRINT 'Cột ThangNam đã tồn tại.';
GO

-- Tên nhân viên P. KD
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TiepNhanThongTin]') AND name = 'TenNVPKD')
BEGIN
    ALTER TABLE [dbo].[TiepNhanThongTin] ADD [TenNVPKD] nvarchar(255) NULL;
    PRINT 'Đã thêm cột TenNVPKD.';
END
ELSE
    PRINT 'Cột TenNVPKD đã tồn tại.';
GO

-- S (kVA), nhiều giá trị cách nhau dấu phẩy
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TiepNhanThongTin]') AND name = 'SkVA')
BEGIN
    ALTER TABLE [dbo].[TiepNhanThongTin] ADD [SkVA] nvarchar(500) NULL;
    PRINT 'Đã thêm cột SkVA.';
END
ELSE
    PRINT 'Cột SkVA đã tồn tại.';
GO

PRINT 'Hoàn tất. Kiểm tra: SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(''TiepNhanThongTin'') AND name IN (''ThangNam'',''TenNVPKD'',''SkVA'');';
