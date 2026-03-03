-- Bổ sung phân quyền cho Tiếp nhận thông tin và Hồ sơ thầu vào bảng PagePermissions
-- Chạy trên database (VD: quanlyphancong). Có thể chạy nhiều lần (idempotent).

-- Tiếp nhận thông tin
IF NOT EXISTS (SELECT 1 FROM [dbo].[PagePermissions] WHERE [PageRoute] = '/tiep-nhan-thong-tin')
BEGIN
    INSERT INTO [dbo].[PagePermissions] ([PageRoute], [PageName], [Description], [IsActive], [CreatedAt])
    VALUES ('/tiep-nhan-thong-tin', N'Tiếp Nhận Thông Tin', N'Quản lý tiếp nhận thông tin', 1, GETUTCDATE());
    PRINT N'Đã thêm page permission: Tiếp Nhận Thông Tin';
END
ELSE
    PRINT N'Page /tiep-nhan-thong-tin đã tồn tại.';
GO

-- Hồ sơ thầu
IF NOT EXISTS (SELECT 1 FROM [dbo].[PagePermissions] WHERE [PageRoute] = '/ho-so-thau')
BEGIN
    INSERT INTO [dbo].[PagePermissions] ([PageRoute], [PageName], [Description], [IsActive], [CreatedAt])
    VALUES ('/ho-so-thau', N'Hồ Sơ Thầu', N'Quản lý hồ sơ thầu', 1, GETUTCDATE());
    PRINT N'Đã thêm page permission: Hồ Sơ Thầu';
END
ELSE
    PRINT N'Page /ho-so-thau đã tồn tại.';
GO

PRINT N'Hoàn tất. Kiểm tra: SELECT * FROM [dbo].[PagePermissions] WHERE [PageRoute] IN (''/tiep-nhan-thong-tin'', ''/ho-so-thau'');';
